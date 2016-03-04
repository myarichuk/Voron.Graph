﻿// -----------------------------------------------------------------------
//  <copyright file="WriteAheadJournal.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using Sparrow;
using Sparrow.Binary;
using Sparrow.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Voron.Data.BTrees;
using Voron.Exceptions;
using Voron.Impl.FileHeaders;
using Voron.Impl.Paging;
using Voron.Util;

namespace Voron.Impl.Journal
{
	public unsafe class WriteAheadJournal : IDisposable
	{
		private readonly StorageEnvironment _env;
		private readonly IVirtualPager _dataPager;

		private long _currentJournalFileSize;
		private DateTime _lastFile;

		private long _journalIndex = -1;

		private bool disposed;

		private readonly LZ4 _lz4 = new LZ4();
		private readonly JournalApplicator _journalApplicator;
		private readonly ModifyHeaderAction _updateLogInfo;

		private ImmutableAppendOnlyList<JournalFile> _files = ImmutableAppendOnlyList<JournalFile>.Empty;
		internal JournalFile CurrentFile;

		private readonly HeaderAccessor _headerAccessor;
		private readonly IVirtualPager _compressionPager;

		public WriteAheadJournal(StorageEnvironment env)
		{
			_env = env;
			_dataPager = _env.Options.DataPager;
			_currentJournalFileSize = env.Options.InitialLogFileSize;
			_headerAccessor = env.HeaderAccessor;
			_updateLogInfo = header =>
			{
				var journalFilesCount = _files.Count;
				var currentJournal = journalFilesCount > 0 ? _journalIndex : -1;
				header->Journal.CurrentJournal = currentJournal;
				header->Journal.JournalFilesCount = journalFilesCount;
				header->IncrementalBackup.LastCreatedJournal = _journalIndex;
			};

			_compressionPager = _env.Options.CreateScratchPager("compression.buffers");
			_journalApplicator = new JournalApplicator(this);
		}

		public ImmutableAppendOnlyList<JournalFile> Files { get { return _files; } }

		public JournalApplicator Applicator { get { return _journalApplicator; } }

		internal long CompressionBufferSize
		{
			get { return _compressionPager.NumberOfAllocatedPages * _compressionPager.PageSize; }
		}

		private JournalFile NextFile(int numberOfPages = 1)
		{
			_journalIndex++;

			var now = DateTime.UtcNow;
			if ((now - _lastFile).TotalSeconds < 90)
			{
				_currentJournalFileSize = Math.Min(_env.Options.MaxLogFileSize, _currentJournalFileSize * 2);
			}
			var actualLogSize = _currentJournalFileSize;
			var minRequiredSize = numberOfPages * _compressionPager.PageSize;
			if (_currentJournalFileSize < minRequiredSize)
			{
				actualLogSize = minRequiredSize;
			}

			_lastFile = now;

			var journalPager = _env.Options.CreateJournalWriter(_journalIndex, actualLogSize);

			var journal = new JournalFile(journalPager, _journalIndex);
			journal.AddRef(); // one reference added by a creator - write ahead log

			_files = _files.Append(journal);

			_headerAccessor.Modify(_updateLogInfo);

			return journal;
		}

		public bool RecoverDatabase(TransactionHeader* txHeader)
		{
			// note, we don't need to do any concurrency here, happens as a single threaded
			// fashion on db startup
			var requireHeaderUpdate = false;

			var logInfo = _headerAccessor.Get(ptr => ptr->Journal);

			if (logInfo.JournalFilesCount == 0)
			{
				_journalIndex = logInfo.LastSyncedJournal;
				return false;
			}

			var oldestLogFileStillInUse = logInfo.CurrentJournal - logInfo.JournalFilesCount + 1;
			if (_env.Options.IncrementalBackupEnabled == false)
			{
				// we want to check that we cleanup old log files if they aren't needed
				// this is more just to be safe than anything else, they shouldn't be there.
				var unusedfiles = oldestLogFileStillInUse;
				while (true)
				{
					unusedfiles--;
					if (_env.Options.TryDeleteJournal(unusedfiles) == false)
						break;
				}

			}

			var lastSyncedTransactionId = logInfo.LastSyncedTransactionId;

			var journalFiles = new List<JournalFile>();
			long lastSyncedTxId = -1;
			long lastSyncedJournal = logInfo.LastSyncedJournal;
			uint lastShippedTxCrc = 0;
			for (var journalNumber = oldestLogFileStillInUse; journalNumber <= logInfo.CurrentJournal; journalNumber++)
			{
				using (var recoveryPager = _env.Options.CreateScratchPager(StorageEnvironmentOptions.JournalRecoveryName(journalNumber)))
				using (var pager = _env.Options.OpenJournalPager(journalNumber))
				{
					RecoverCurrentJournalSize(pager);

					var transactionHeader = txHeader->TransactionId == 0 ? null : txHeader;
					var journalReader = new JournalReader(pager, recoveryPager, lastSyncedTransactionId, transactionHeader);
					journalReader.RecoverAndValidate(_env.Options);

					var pagesToWrite = journalReader
						.TransactionPageTranslation
						.Select(kvp => recoveryPager.Read(kvp.Value.JournalPos))
						.OrderBy(x => x.PageNumber)
						.ToList();

					var lastReadHeaderPtr = journalReader.LastTransactionHeader;

					if (lastReadHeaderPtr != null)
					{
						if (pagesToWrite.Count > 0)
							ApplyPagesToDataFileFromJournal(pagesToWrite);

						*txHeader = *lastReadHeaderPtr;
						lastSyncedTxId = txHeader->TransactionId;
						lastShippedTxCrc = txHeader->Crc;
						lastSyncedJournal = journalNumber;
					}

					if (journalReader.RequireHeaderUpdate || journalNumber == logInfo.CurrentJournal)
					{
						var jrnlWriter = _env.Options.CreateJournalWriter(journalNumber, pager.NumberOfAllocatedPages * _compressionPager.PageSize);
						var jrnlFile = new JournalFile(jrnlWriter, journalNumber);
						jrnlFile.InitFrom(journalReader);
						jrnlFile.AddRef(); // creator reference - write ahead log

						journalFiles.Add(jrnlFile);
					}

					if (journalReader.RequireHeaderUpdate) //this should prevent further loading of transactions
					{
						requireHeaderUpdate = true;
						break;
					}
				}
			}

			_files = _files.AppendRange(journalFiles);
			
			Debug.Assert(lastSyncedTxId >= 0);
			Debug.Assert(lastSyncedJournal >= 0);

			_journalIndex = lastSyncedJournal;

			_headerAccessor.Modify(
				header =>
				{
					header->Journal.CurrentJournal = lastSyncedJournal;
					header->Journal.JournalFilesCount = _files.Count;
					header->IncrementalBackup.LastCreatedJournal = _journalIndex;
				});

			CleanupInvalidJournalFiles(lastSyncedJournal);
			CleanupUnusedJournalFiles(oldestLogFileStillInUse, lastSyncedJournal);

			if (_files.Count > 0)
			{
				var lastFile = _files.Last();
				if (lastFile.AvailablePages >= 2)
					// it must have at least one page for the next transaction header and one page for data
					CurrentFile = lastFile;
			}

			return requireHeaderUpdate;
		}

		private void CleanupUnusedJournalFiles(long oldestLogFileStillInUse, long lastSyncedJournal)
		{
			var logFile = oldestLogFileStillInUse;
			while (logFile < lastSyncedJournal)
			{
				_env.Options.TryDeleteJournal(logFile);
				logFile++;
			}
		}

		private void CleanupInvalidJournalFiles(long lastSyncedJournal)
		{
			// we want to check that we cleanup newer log files, since everything from
			// the current file is considered corrupted
			var badJournalFiles = lastSyncedJournal;
			while (true)
			{
				badJournalFiles++;
				if (_env.Options.TryDeleteJournal(badJournalFiles) == false)
				{
					break;
				}
			}
		}

		private void ApplyPagesToDataFileFromJournal(List<TreePage> sortedPagesToWrite)
		{
			var last = sortedPagesToWrite.Last();

			var numberOfPagesInLastPage = last.IsOverflow == false ? 1 :
				_env.Options.DataPager.GetNumberOfOverflowPages(last.OverflowSize);

			_dataPager.EnsureContinuous(last.PageNumber, numberOfPagesInLastPage);

		    _dataPager.MaybePrefetchMemory(sortedPagesToWrite);

			foreach (var page in sortedPagesToWrite)
			{
				_dataPager.Write(page);
			}
		}

		private void RecoverCurrentJournalSize(IVirtualPager pager)
		{
			var journalSize = Bits.NextPowerOf2(pager.NumberOfAllocatedPages * pager.PageSize);
			if (journalSize >= _env.Options.MaxLogFileSize) // can't set for more than the max log file size
				return;

			// this set the size of the _next_ journal file size
			_currentJournalFileSize = Math.Min(journalSize, _env.Options.MaxLogFileSize);
		}


		public Page ReadPage(LowLevelTransaction tx, long pageNumber, Dictionary<int, PagerState> scratchPagerStates)
		{
			// read transactions have to read from journal snapshots
			if (tx.Flags == TransactionFlags.Read)
			{
				// read log snapshots from the back to get the most recent version of a page
				for (var i = tx.JournalSnapshots.Count - 1; i >= 0; i--)
				{
					PagePosition value;
					if (tx.JournalSnapshots[i].PageTranslationTable.TryGetValue(tx, pageNumber, out value))
					{
						var page = _env.ScratchBufferPool.ReadPage(value.ScratchNumber, value.ScratchPos, scratchPagerStates[value.ScratchNumber]);

						Debug.Assert(page.PageNumber == pageNumber);

						return page;
					}
				}

				return null;
			}

			// write transactions can read directly from journals
			var files = _files;
			for (var i = files.Count - 1; i >= 0; i--)
			{
				PagePosition value;
				if (files[i].PageTranslationTable.TryGetValue(tx, pageNumber, out value))
				{
					var page = _env.ScratchBufferPool.ReadPage(value.ScratchNumber, value.ScratchPos);

					Debug.Assert(page.PageNumber == pageNumber);

					return page;
				}
			}

			return null;
		}


		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;

			// we cannot dispose the journal until we are done with all of the pending writes

			_compressionPager.Dispose();
			_lz4.Dispose();

            _journalApplicator.Dispose();
            if (_env.Options.OwnsPagers)
			{
				foreach (var logFile in _files)
				{
					logFile.Dispose();
				}

			}
			else
			{
				foreach (var logFile in _files)
				{
					GC.SuppressFinalize(logFile);
				}

			}

			_files = ImmutableAppendOnlyList<JournalFile>.Empty;
		}

		public JournalInfo GetCurrentJournalInfo()
		{
			return _headerAccessor.Get(ptr => ptr->Journal);
		}

		public List<JournalSnapshot> GetSnapshots()
		{
			return _files.Select(x => x.GetSnapshot()).ToList();
		}

        public void Clear(LowLevelTransaction tx)
		{
			if (tx.Flags != TransactionFlags.ReadWrite)
				throw new InvalidOperationException("Clearing of write ahead journal should be called only from a write transaction");

			foreach (var journalFile in _files)
			{
				journalFile.Release();
			}
			_files = ImmutableAppendOnlyList<JournalFile>.Empty;
			CurrentFile = null;
		}



		public class JournalSyncEventArgs : EventArgs
		{
			public long OldestTransactionId { get; private set; }

			public JournalSyncEventArgs(long oldestTransactionId)
			{
				OldestTransactionId = oldestTransactionId;
			}
		}

		public class JournalApplicator : IDisposable
		{
			private const long DelayedDataFileSynchronizationBytesLimit = 2L * 1024 * 1024 * 1024;
			private readonly TimeSpan _delayedDataFileSynchronizationTimeLimit = TimeSpan.FromMinutes(1);
			private readonly Dictionary<long, JournalFile> _journalsToDelete = new Dictionary<long, JournalFile>();
			private readonly object _flushingLock = new object();
			private readonly WriteAheadJournal _waj;
			private long _lastSyncedTransactionId;
			private long _lastSyncedJournal;
			private long _totalWrittenButUnsyncedBytes;
			private DateTime _lastDataFileSyncTime;
			private JournalFile _lastFlushedJournal;
			private long? forcedIterateJournalsAsOf = null;
			private bool forcedFlushOfOldPages = false;
			private bool ignoreLockAlreadyTaken = false;

			public JournalApplicator(WriteAheadJournal waj)
			{
				_waj = waj;
			}

			private IDisposable ForceFlushingPagesOlderThan(long oldestActiveTransaction)
			{
				forcedIterateJournalsAsOf = oldestActiveTransaction == 0 ?
															long.MaxValue : // if there is no active transaction, let it read as of LastTransaction from a snapshot
															oldestActiveTransaction - 1;
				forcedFlushOfOldPages = true;
				ignoreLockAlreadyTaken = true;

				return new DisposableAction(() =>
				{
					forcedIterateJournalsAsOf = null;
					forcedFlushOfOldPages = false;
					ignoreLockAlreadyTaken = false;
				});
			}

			public void ApplyLogsToDataFile(long oldestActiveTransaction, CancellationToken token, LowLevelTransaction transaction = null, bool allowToFlushOverwrittenPages = false)
			{
				if (token.IsCancellationRequested)
					return;

				if (Monitor.IsEntered(_flushingLock) && ignoreLockAlreadyTaken == false)
					throw new InvalidJournalFlushRequestException("Applying journals to the data file has been already requested on the same thread");

				bool lockTaken = false;

				try
				{
					Monitor.TryEnter(_flushingLock, Debugger.IsAttached ? TimeSpan.FromMinutes(30) : TimeSpan.FromSeconds(30), ref lockTaken);

					if (lockTaken == false)
						throw new TimeoutException("Could not acquire the write lock in 30 seconds");

					var alreadyInWriteTx = transaction != null && transaction.Flags == TransactionFlags.ReadWrite;

					var jrnls = _waj._files.Select(x => x.GetSnapshot()).OrderBy(x => x.Number).ToList();
					if (jrnls.Count == 0)
						return; // nothing to do

					Debug.Assert(jrnls.First().Number >= _lastSyncedJournal);

					var pagesToWrite = new Dictionary<long, PagePosition>();

					long lastProcessedJournal = -1;
					long previousJournalMaxTransactionId = -1;

					long lastFlushedTransactionId = -1;

					foreach (var journalFile in jrnls.Where(x => x.Number >= _lastSyncedJournal))
					{
						var currentJournalMaxTransactionId = -1L;

						var iterateLatestAsOf = journalFile.LastTransaction;

						if (forcedFlushOfOldPages && forcedIterateJournalsAsOf.HasValue)
							iterateLatestAsOf = Math.Min(journalFile.LastTransaction, forcedIterateJournalsAsOf.Value);

						foreach (var pagePosition in journalFile.PageTranslationTable.IterateLatestAsOf(iterateLatestAsOf))
						{
							if (oldestActiveTransaction != 0 &&
								pagePosition.Value.TransactionId >= oldestActiveTransaction)
							{
								if(pagePosition.Value.IsFreedPageMarker)
									continue;

								// we cannot write this yet, there is a read transaction that might be looking at this
								// however, we _aren't_ going to be writing this to the data file, since that would be a 
								// waste, we would just overwrite that value in the next flush anyway
								PagePosition existingPagePosition;
								if (pagesToWrite.TryGetValue(pagePosition.Key, out existingPagePosition) &&
									pagePosition.Value.JournalNumber == existingPagePosition.JournalNumber)
								{
									// remove the page only when it comes from the same journal
									// otherwise we can damage the journal's page translation table (PTT)
									// because the existing overwrite in a next journal can be filtered out
									// so we wouldn't write any page to the data file
									pagesToWrite.Remove(pagePosition.Key);
								}

								continue;
							}

							if (pagePosition.Value.IsFreedPageMarker)
							{
								// to ensure that we won't overwrite data by a page from the older journal where it wasn't marked as free 
								// while now it not being considered to be applied to the data file
								pagesToWrite.Remove(pagePosition.Key); 
								continue;
							}

							if (journalFile.Number == _lastSyncedJournal && pagePosition.Value.TransactionId <= _lastSyncedTransactionId)
								continue;

							currentJournalMaxTransactionId = Math.Max(currentJournalMaxTransactionId, pagePosition.Value.TransactionId);

							if (currentJournalMaxTransactionId < previousJournalMaxTransactionId)
								throw new InvalidOperationException(
									"Journal applicator read beyond the oldest active transaction in the next journal file. " +
									"This should never happen. Current journal max tx id: " + currentJournalMaxTransactionId +
									", previous journal max ix id: " + previousJournalMaxTransactionId +
									", oldest active transaction: " + oldestActiveTransaction);


							lastProcessedJournal = journalFile.Number;
							pagesToWrite[pagePosition.Key] = pagePosition.Value;

							lastFlushedTransactionId = currentJournalMaxTransactionId;
						}

						if (currentJournalMaxTransactionId == -1L)
							continue;

						previousJournalMaxTransactionId = currentJournalMaxTransactionId;
					}

					if (pagesToWrite.Count == 0)
					{
						if (allowToFlushOverwrittenPages)
						{
							// we probably filtered out all pages because they have some overwrites and we applied an optimization
							// that relays on iterating over pages from end of PTT, however we might want to flush such pages
							// in order to allow to free them because the scratch buffer might require them
							// so we can flush all pages that we are sure they aren't being read by any transaction

							using (ForceFlushingPagesOlderThan(oldestActiveTransaction))
							{
								ApplyLogsToDataFile(oldestActiveTransaction, token, transaction, false);
							}
						}

						return;
					}

					try
					{
						ApplyPagesToDataFileFromScratch(pagesToWrite, transaction, alreadyInWriteTx);
					}
					catch (DiskFullException diskFullEx)
					{
						_waj._env.HandleDataDiskFullException(diskFullEx);
						return;
					}

					var unusedJournals = GetUnusedJournalFiles(jrnls, lastProcessedJournal, lastFlushedTransactionId);

					foreach (var unused in unusedJournals.Where(unused => !_journalsToDelete.ContainsKey(unused.Number)))
					{
						_journalsToDelete.Add(unused.Number, unused);
					}

					using (var txw = alreadyInWriteTx ? null : _waj._env.NewLowLevelTransaction(TransactionFlags.ReadWrite).JournalApplicatorTransaction())
					{
						_lastSyncedJournal = lastProcessedJournal;
						_lastSyncedTransactionId = lastFlushedTransactionId;

						_lastFlushedJournal = _waj._files.First(x => x.Number == _lastSyncedJournal);
						
						if (unusedJournals.Count > 0)
						{
							var lastUnusedJournalNumber = unusedJournals.Last().Number;
							_waj._files = _waj._files.RemoveWhile(x => x.Number <= lastUnusedJournalNumber);
						}

						if (_waj._files.Count == 0)
							_waj.CurrentFile = null;

						FreeScratchPages(unusedJournals, txw ?? transaction);

						if (txw != null)
							txw.Commit();
					}
					
					if (_totalWrittenButUnsyncedBytes > DelayedDataFileSynchronizationBytesLimit ||
						DateTime.UtcNow - _lastDataFileSyncTime > _delayedDataFileSynchronizationTimeLimit)
					{
						SyncDataFile(oldestActiveTransaction);
					}

				}
				finally
				{
					if(lockTaken)
						Monitor.Exit(_flushingLock);
				}
			}

			internal void SyncDataFile(long oldestActiveTransaction)
			{
				_waj._dataPager.Sync();

				UpdateFileHeaderAfterDataFileSync(_lastFlushedJournal, oldestActiveTransaction);

				foreach (var toDelete in _journalsToDelete.Values)
				{
					if (_waj._env.Options.IncrementalBackupEnabled == false)
						toDelete.DeleteOnClose = true;

					toDelete.Release();
				}

				_journalsToDelete.Clear();
				_totalWrittenButUnsyncedBytes = 0;
				_lastDataFileSyncTime = DateTime.UtcNow;
			}

            public Dictionary<long, int> writtenPages = new Dictionary<long, int>(NumericEqualityComparer.Instance);

            private void ApplyPagesToDataFileFromScratch(Dictionary<long, PagePosition> pagesToWrite, LowLevelTransaction transaction, bool alreadyInWriteTx)
			{
				var scratchBufferPool = _waj._env.ScratchBufferPool;
				var scratchPagerStates = new Dictionary<int, PagerState>();

				try
				{
					var sortedPages = pagesToWrite.OrderBy(x => x.Key)
													.Select(x =>
													{
														var scratchNumber = x.Value.ScratchNumber;

														PagerState pagerState;
														if(scratchPagerStates.TryGetValue(scratchNumber, out pagerState) == false)
														{
															pagerState = scratchBufferPool.GetPagerState(scratchNumber);
															pagerState.AddRef();

															scratchPagerStates.Add(scratchNumber, pagerState);
														}

														return scratchBufferPool.ReadPage(scratchNumber, x.Value.ScratchPos, pagerState);
													})
													.ToList();

					var last = sortedPages.Last();

					var numberOfPagesInLastPage = last.IsOverflow == false ? 1 :
						_waj._env.Options.DataPager.GetNumberOfOverflowPages(last.OverflowSize);

					EnsureDataPagerSpacing(transaction, last, numberOfPagesInLastPage, alreadyInWriteTx);

					long written = 0;
					int index = 0;
					foreach (var page in sortedPages)
					{
						written += _waj._dataPager.WritePage(page);
						index++;
					}

					_totalWrittenButUnsyncedBytes += written;
				}
				finally
				{
					foreach (var scratchPagerState in scratchPagerStates.Values)
					{
						scratchPagerState.Release();
					}
				}
			}

            private void EnsureDataPagerSpacing(LowLevelTransaction transaction, Page last, int numberOfPagesInLastPage,
					bool alreadyInWriteTx)
			{
				if (_waj._dataPager.WillRequireExtension(last.PageNumber, numberOfPagesInLastPage) == false)
					return;

				if (alreadyInWriteTx)
				{
				    var pagerState = _waj._dataPager.EnsureContinuous(last.PageNumber, numberOfPagesInLastPage);
                    transaction.AddPagerState(pagerState);
                }
				else
				{
					using (var tx = _waj._env.NewLowLevelTransaction(TransactionFlags.ReadWrite).JournalApplicatorTransaction())
					{
					    var pagerState = _waj._dataPager.EnsureContinuous(last.PageNumber, numberOfPagesInLastPage);
                        tx.AddPagerState(pagerState);

                        tx.Commit();
					}
				}
			}

			private void FreeScratchPages(IEnumerable<JournalFile> unusedJournalFiles, LowLevelTransaction txw)
			{
				// we have to free pages of the unused journals before the remaining ones that are still in use
				// to prevent reading from them by any read transaction (read transactions search journals from the newest
				// to read the most updated version)
				foreach (var journalFile in unusedJournalFiles.OrderBy(x => x.Number))
				{
					journalFile.FreeScratchPagesOlderThan(txw, _lastSyncedTransactionId, forceToFreeAllPages: forcedFlushOfOldPages);
				}

				foreach (var jrnl in _waj._files.OrderBy(x => x.Number))
				{
					jrnl.FreeScratchPagesOlderThan(txw, _lastSyncedTransactionId, forceToFreeAllPages: forcedFlushOfOldPages);
				}
			}

			private List<JournalFile> GetUnusedJournalFiles(IEnumerable<JournalSnapshot> jrnls, long lastProcessedJournal, long lastFlushedTransactionId)
			{
				var unusedJournalFiles = new List<JournalFile>();
				foreach (var j in jrnls)
				{
					if (j.Number > lastProcessedJournal) // after the last log we synced, nothing to do here
						continue;
					if (j.Number == lastProcessedJournal) // we are in the last log we synced
					{
						if (j.AvailablePages != 0 || //　if there are more pages to be used here or 
						j.PageTranslationTable.MaxTransactionId() != lastFlushedTransactionId) // we didn't synchronize whole journal
							continue; // do not mark it as unused
					}
					unusedJournalFiles.Add(_waj._files.First(x => x.Number == j.Number));
				}
				return unusedJournalFiles;
			}

			public void UpdateFileHeaderAfterDataFileSync(JournalFile file, long oldestActiveTransaction)
			{
				var txHeaders = stackalloc TransactionHeader[2];
				var readTxHeader = &txHeaders[0];
				var lastReadTxHeader = txHeaders[1];

				var txPos = 0;
				while (true)
				{
					if (file.ReadTransaction(txPos, readTxHeader) == false)
						break;
					if (readTxHeader->HeaderMarker != Constants.TransactionHeaderMarker)
						break;
					if (readTxHeader->TransactionId + 1 == oldestActiveTransaction)
						break;

					lastReadTxHeader = *readTxHeader;

					var compressedPages = (readTxHeader->CompressedSize / _waj._env.Options.PageSize) + (readTxHeader->CompressedSize % _waj._env.Options.PageSize == 0 ? 0 : 1);

					txPos += compressedPages + 1;
				}
				
				Debug.Assert(_lastSyncedJournal != -1);
				Debug.Assert(_lastSyncedTransactionId != -1);

				_waj._headerAccessor.Modify(header =>
				{
					header->TransactionId = lastReadTxHeader.TransactionId;
					header->LastPageNumber = lastReadTxHeader.LastPageNumber;

					header->Journal.LastSyncedJournal = _lastSyncedJournal;
					header->Journal.LastSyncedTransactionId = _lastSyncedTransactionId;

					header->Root = lastReadTxHeader.Root;

					_waj._updateLogInfo(header);
				});
			}

			public void Dispose()
			{
				foreach (var journalFile in _journalsToDelete)
				{
					// we need to release all unused journals 
					// however here we don't force them to DeleteOnClose
					// because we didn't synced the data file yet
					// and we will need them on a next database recovery
					journalFile.Value.Release();
				}
			}

			public bool IsCurrentThreadInFlushOperation
			{
				get { return Monitor.IsEntered(_flushingLock); }
			}

			public IDisposable TryTakeFlushingLock(ref bool lockTaken)
			{
				Monitor.TryEnter(_flushingLock, ref lockTaken);
				bool localLockTaken = lockTaken;

				ignoreLockAlreadyTaken = true;

				return new DisposableAction(() =>
				{
					ignoreLockAlreadyTaken = false;
					if (localLockTaken)
						Monitor.Exit(_flushingLock);
				});
			}

			public IDisposable TakeFlushingLock()
			{
				bool lockTaken = false;
				Monitor.Enter(_flushingLock, ref lockTaken);
				ignoreLockAlreadyTaken = true;

				return new DisposableAction(() =>
				{
					ignoreLockAlreadyTaken = false;
					if (lockTaken)
						Monitor.Exit(_flushingLock);
				});
			}

			internal void DeleteCurrentAlreadyFlushedJournal()
			{
				if (_waj._env.Options.IncrementalBackupEnabled)
					return;

				if (_waj._files.Count == 0)
					return;

				if (_waj._files.Count != 1)
					throw new InvalidOperationException("Cannot delete current journal because there is more journals being in use");

				var current = _waj._files.First();

				if (current.Number != _lastSyncedJournal)
					throw new InvalidOperationException(string.Format("Cannot delete current journal because it isn't last synced file. Current journal number: {0}, the last one which was synced {1}", _waj.CurrentFile.Number, _lastSyncedJournal));

				if(_waj._env.NextWriteTransactionId - 1 != _lastSyncedTransactionId)
					throw new InvalidOperationException();
					
				_waj._files = _waj._files.RemoveFront(1);
				_waj.CurrentFile = null;

				current.DeleteOnClose = true;
				current.Release();

				_waj._headerAccessor.Modify(header => _waj._updateLogInfo(header));
			}
		}

		public void WriteToJournal(LowLevelTransaction tx, int pageCount)
		{
			var pages = CompressPages(tx, pageCount, _compressionPager);

			if (CurrentFile == null || CurrentFile.AvailablePages < pages.Length)
			{
				CurrentFile = NextFile(pages.Length);
			}

			CurrentFile.Write(tx, pages);

			if (CurrentFile.AvailablePages == 0)
				CurrentFile = null;
		}

        private IntPtr[] CompressPages(LowLevelTransaction tx, int numberOfPages, IVirtualPager compressionPager)
		{
			// numberOfPages include the tx header page, which we don't compress
			var dataPagesCount = numberOfPages - 1;
			var pageSize = tx.Environment.Options.PageSize;
			var sizeInBytes = dataPagesCount * pageSize;
			var outputBuffer = LZ4.MaximumOutputLength(sizeInBytes);
			var outputBufferInPages = outputBuffer / pageSize +
									  (outputBuffer % pageSize == 0 ? 0 : 1);
			var pagesRequired = (dataPagesCount + outputBufferInPages);

		    var pagerState = compressionPager.EnsureContinuous(0, pagesRequired);
            tx.AddPagerState(pagerState);
            var tempBuffer = compressionPager.AcquirePagePointer(0);
			var compressionBuffer = compressionPager.AcquirePagePointer(dataPagesCount);

			var write = tempBuffer;
			var txPages = tx.GetTransactionPages();

            foreach( var txPage in txPages )
            {
                var scratchPage = tx.Environment.ScratchBufferPool.AcquirePagePointer(txPage.ScratchFileNumber, txPage.PositionInScratchBuffer);
                var count = txPage.NumberOfPages * pageSize;
                Memory.BulkCopy(write, scratchPage, count);
                write += count;
            }

			var len = DoCompression(tempBuffer, compressionBuffer, sizeInBytes, outputBuffer);
			var remainder = len % pageSize;
			var compressedPages = (len / pageSize) + (remainder == 0 ? 0 : 1);

		    if (remainder != 0)
		    {
                // zero the remainder of the page
				UnmanagedMemory.Set(compressionBuffer + len, 0, remainder);
		    }

			var pages = new IntPtr[compressedPages + 1];

            var txHeaderPage = tx.GetTransactionHeaderPage();
            var txHeaderBase = tx.Environment.ScratchBufferPool.AcquirePagePointer(txHeaderPage.ScratchFileNumber, txHeaderPage.PositionInScratchBuffer);
			var txHeader = (TransactionHeader*)txHeaderBase;

			txHeader->Compressed = true;
			txHeader->CompressedSize = len;
			txHeader->UncompressedSize = sizeInBytes;

			pages[0] = new IntPtr(txHeaderBase);
			for (int index = 0; index < compressedPages; index++)
			{
				pages[index + 1] = new IntPtr(compressionBuffer + (index * pageSize));
			}

			txHeader->Crc = Crc.Value(compressionBuffer, 0, compressedPages * pageSize);

			return pages;
		}


		private int DoCompression(byte* input, byte* output, int inputLength, int outputLength)
		{
			var doCompression = _lz4.Encode64(
				input,
				output,
				inputLength,
				outputLength);

			return doCompression;
		}
	}
}
