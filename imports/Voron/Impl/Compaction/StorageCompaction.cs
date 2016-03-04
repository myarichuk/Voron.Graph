﻿// -----------------------------------------------------------------------
//  <copyright file="Compaction.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------
using System;
using System.IO;
using Voron.Data;
using Voron.Data.BTrees;
using Voron.Impl.FreeSpace;

namespace Voron.Impl.Compaction
{
    public class CompactionProgress
    {
        public string TreeName;

        public long CopiedTrees;
        public long TotalTreeCount;

        public long CopiedTreeRecords;
        public long TotalTreeRecordsCount;
    }

    public unsafe static class StorageCompaction
    {
        public const string CannotCompactBecauseOfIncrementalBackup = "Cannot compact a storage that supports incremental backups. The compact operation changes internal data structures on which the incremental backup relays.";

        public static void Execute(StorageEnvironmentOptions srcOptions, StorageEnvironmentOptions.DirectoryStorageEnvironmentOptions compactOptions, Action<CompactionProgress> progressReport = null)
        {
            if (srcOptions.IncrementalBackupEnabled)
                throw new InvalidOperationException(CannotCompactBecauseOfIncrementalBackup);

            long minimalCompactedDataFileSize;

            srcOptions.ManualFlushing = true; // prevent from flushing during compaction - we shouldn't touch any source files
            compactOptions.ManualFlushing = true; // let us flush manually during data copy

            using (var existingEnv = new StorageEnvironment(srcOptions))
            using (var compactedEnv = new StorageEnvironment(compactOptions))
            {
                CopyTrees(existingEnv, compactedEnv, progressReport);

                compactedEnv.FlushLogToDataFile(allowToFlushOverwrittenPages: true);

                compactedEnv.Journal.Applicator.SyncDataFile(compactedEnv.OldestTransaction);
                compactedEnv.Journal.Applicator.DeleteCurrentAlreadyFlushedJournal();

                minimalCompactedDataFileSize = compactedEnv.NextPageNumber * existingEnv.Options.PageSize;
            }

            using (var compactedDataFile = new FileStream(Path.Combine(compactOptions.BasePath, Constants.DatabaseFilename), FileMode.Open, FileAccess.ReadWrite))
            {
                compactedDataFile.SetLength(minimalCompactedDataFileSize);
            }
        }

        private static void CopyTrees(StorageEnvironment existingEnv, StorageEnvironment compactedEnv, Action<CompactionProgress> progressReport = null)
        {
            using (var txr = existingEnv.ReadTransaction())
            using (var rootIterator = txr.LowLevelTransaction.RootObjects.Iterate())
            {
                if (rootIterator.Seek(Slice.BeforeAllKeys) == false)
                    return;

                var totalTreesCount = txr.LowLevelTransaction.RootObjects.State.NumberOfEntries;
                var copiedTrees = 0L;
                do
                {
                    var treeName = rootIterator.CurrentKey.ToString();
                    var objectType = txr.GetRootObjectType(rootIterator.CurrentKey);
                    switch (objectType)
                    {
                        case RootObjectType.None:
                            break;
                        case RootObjectType.VariableSizeTree:
                            copiedTrees = CopyVariableSizeTree(compactedEnv, progressReport, txr, treeName, copiedTrees, totalTreesCount);
                            break;
                        case RootObjectType.EmbeddedFixedSizeTree:
                        case RootObjectType.FixedSizeTree:
                            if (FreeSpaceHandling.IsFreeSpaceTreeName(treeName))
                            {
                                copiedTrees++;// we don't copy the fixed size tree
                                continue;
                            }
                            copiedTrees = CopyFixedSizeTrees(compactedEnv, progressReport, txr, rootIterator, treeName, copiedTrees, totalTreesCount);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("Unknown " + objectType);
                    }

                } while (rootIterator.MoveNext());
            }
        }

        private static long CopyFixedSizeTrees(StorageEnvironment compactedEnv, Action<CompactionProgress> progressReport, Transaction txr,
            TreeIterator rootIterator, string treeName, long copiedTrees, long totalTreesCount)
        {
            
            var fst = txr.FixedTreeFor(rootIterator.CurrentKey, 0);
            Report(treeName, copiedTrees, totalTreesCount, 0,
                fst.NumberOfEntries,
                progressReport);
            using (var it = fst.Iterate())
            {
                var copiedEntries = 0L;
                if (it.Seek(Int64.MinValue) == false)
                    return copiedTrees;
                do
                {
                    using (var txw = compactedEnv.WriteTransaction())
                    {
                        var snd = txw.FixedTreeFor(rootIterator.CurrentKey);
                        var transactionSize = 0L;
                        do
                        {
                            snd.Add(it.CurrentKey, it.Value);
                            transactionSize += fst.ValueSize + sizeof (long);
                            copiedEntries++;
                        } while (transactionSize < compactedEnv.Options.MaxLogFileSize/2 && it.MoveNext());

                        txw.Commit();
                    }
                    if (fst.NumberOfEntries == copiedEntries)
                        copiedTrees++;

                    Report(treeName, copiedTrees, totalTreesCount, copiedEntries,
                        fst.NumberOfEntries,
                        progressReport);
                    compactedEnv.FlushLogToDataFile();
                } while (it.MoveNext());
            }
            return copiedTrees;
        }

        private static unsafe long CopyVariableSizeTree(StorageEnvironment compactedEnv, Action<CompactionProgress> progressReport, Transaction txr,
            string treeName, long copiedTrees, long totalTreesCount)
        {
            var existingTree = txr.ReadTree(treeName);

            Report(treeName, copiedTrees, totalTreesCount, 0, existingTree.State.NumberOfEntries, progressReport);

            using (var existingTreeIterator = existingTree.Iterate())
            {
                if (existingTreeIterator.Seek(Slice.BeforeAllKeys) == false)
                    return copiedTrees;

                using (var txw = compactedEnv.WriteTransaction())
                {
                    txw.CreateTree(treeName);
                    txw.Commit();
                }

                var copiedEntries = 0L;

                do
                {
                    var transactionSize = 0L;

                    using (var txw = compactedEnv.WriteTransaction())
                    {
                        var newTree = txw.ReadTree(treeName);

                        do
                        {
                            var key = existingTreeIterator.CurrentKey;

                            if (existingTreeIterator.Current->Flags == TreeNodeFlags.MultiValuePageRef)
                            {
                                using (var multiTreeIterator = existingTree.MultiRead(key))
                                {
                                    if (multiTreeIterator.Seek(Slice.BeforeAllKeys) == false)
                                        continue;

                                    do
                                    {
                                        var multiValue = multiTreeIterator.CurrentKey;
                                        newTree.MultiAdd(key, multiValue);
                                        transactionSize += multiValue.Size;
                                    } while (multiTreeIterator.MoveNext());
                                }
                            }
                            else
                            {
                                using (var value = existingTree.Read(key).Reader.AsStream())
                                {
                                    newTree.Add(key, value);
                                    transactionSize += value.Length;
                                }
                            }

                            copiedEntries++;
                        } while (transactionSize < compactedEnv.Options.MaxLogFileSize / 2 && existingTreeIterator.MoveNext());

                        txw.Commit();
                    }

                    if (copiedEntries == existingTree.State.NumberOfEntries)
                        copiedTrees++;

                    Report(treeName, copiedTrees, totalTreesCount, copiedEntries, existingTree.State.NumberOfEntries,
                        progressReport);

                    compactedEnv.FlushLogToDataFile();
                } while (existingTreeIterator.MoveNext());
            }
            return copiedTrees;
        }

        private static void Report(string treeName, long copiedTrees, long totalTreeCount, long copiedRecords, long totalTreeRecordsCount, Action<CompactionProgress> progressReport)
        {
            if (progressReport == null)
                return;

            progressReport(new CompactionProgress
            {
                TreeName = treeName,
                CopiedTrees = copiedTrees,
                TotalTreeCount = totalTreeCount,
                CopiedTreeRecords = copiedRecords,
                TotalTreeRecordsCount = totalTreeRecordsCount
            });
        }
    }
}