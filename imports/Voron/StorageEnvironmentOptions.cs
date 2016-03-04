﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Sparrow;
using Voron.Impl;
using Voron.Impl.FileHeaders;
using Voron.Impl.Journal;
using Voron.Impl.Paging;
using Voron.Platform.Posix;
using Voron.Platform.Win32;
using Voron.Util;

namespace Voron
{
    public abstract class StorageEnvironmentOptions : IDisposable
    {
        public event EventHandler<RecoveryErrorEventArgs> OnRecoveryError;

        public void InvokeRecoveryError(object sender, string message, Exception e)
        {
            var handler = OnRecoveryError;
            if (handler == null)
            {
                throw new InvalidDataException(message + Environment.NewLine +
                     "An exception has been thrown because there isn't a listener to the OnRecoveryError event on the storage options.", e);
            }

            handler(this, new RecoveryErrorEventArgs(message, e));
        }

        public Action<long> OnScratchBufferSizeChanged = delegate { };

        public long? InitialFileSize { get; set; }

        public long MaxLogFileSize
        {
            get { return _maxLogFileSize; }
            set
            {
                if (value < _initialLogFileSize)
                    InitialLogFileSize = value;
                _maxLogFileSize = value;
            }
        }

        public long InitialLogFileSize
        {
            get { return _initialLogFileSize; }
            set
            {
                if (value > MaxLogFileSize)
                    MaxLogFileSize = value;
                _initialLogFileSize = value;
            }
        }

        public int PageSize
        {
            get { return _pageSize; }
            set
            {
                if (value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value), "PageSize must be less than " + ushort.MaxValue);
                if (value < 4096)
                    throw new ArgumentOutOfRangeException(nameof(value), "PageSize must be higher than 4096 bytes");

                if (value % 512 != 0)
                    throw new ArgumentException("PageSize must be evenly divisible by 512 (sector size)", nameof(value));
                _pageSize = value;
            }
        }

        // if set to a non zero value, will check that the expected schema is there
        public int SchemaVersion { get; set; }

        public long MaxScratchBufferSize { get; set; }

        public long MaxNumberOfPagesInMergedTransaction { get; set; }

        public bool OwnsPagers { get; set; }

        public bool ManualFlushing { get; set; }

        public bool IncrementalBackupEnabled { get; set; }

        public abstract IVirtualPager DataPager { get; }

        public long MaxNumberOfPagesInJournalBeforeFlush { get; set; }

        public int IdleFlushTimeout { get; set; }

        public long? MaxStorageSize { get; set; }
        public abstract string BasePath { get; }

        public abstract IJournalWriter CreateJournalWriter(long journalNumber, long journalSize);

        protected bool Disposed;
        private long _initialLogFileSize;
        private long _maxLogFileSize;
        private int _pageSize;

        public Func<string, bool> ShouldUseKeyPrefix { get; set; }

        protected StorageEnvironmentOptions()
        {
            PageSize = 4096;
            ShouldUseKeyPrefix = name => false;
            MaxNumberOfPagesInJournalBeforeFlush = 1024; // 4 MB

            IdleFlushTimeout = 5000; // 5 seconds

            MaxLogFileSize = 64 * 1024 * 1024;

            InitialLogFileSize = 64 * 1024;

            MaxScratchBufferSize = 512 * 1024 * 1024;

            ScratchBufferOverflowTimeout = 5000;

            MaxNumberOfPagesInMergedTransaction = 1024 * 128;// Ends up being 512 MB

            OwnsPagers = true;
            IncrementalBackupEnabled = false;
        }

        public int ScratchBufferOverflowTimeout { get; set; }

        
        public static StorageEnvironmentOptions CreateMemoryOnly(string configTempPath = null)
        {
             if (configTempPath == null)
                configTempPath = Path.GetTempPath();

            return new PureMemoryStorageEnvironmentOptions(configTempPath);
        }

        public static StorageEnvironmentOptions ForPath(string path, string tempPath = null, string journalPath = null)
        {
            return new DirectoryStorageEnvironmentOptions(path, tempPath, journalPath);
        }

        public IDisposable AllowManualFlushing()
        {
            var old = ManualFlushing;
            ManualFlushing = true;

            return new DisposableAction(() => ManualFlushing = old);
        }


        public class DirectoryStorageEnvironmentOptions : StorageEnvironmentOptions
        {
            private readonly string _journalPath;
            private readonly string _basePath;
            private readonly string _tempPath;

            private readonly Lazy<IVirtualPager> _dataPager;

            private readonly ConcurrentDictionary<string, Lazy<IJournalWriter>> _journals =
                new ConcurrentDictionary<string, Lazy<IJournalWriter>>(StringComparer.OrdinalIgnoreCase);

            public DirectoryStorageEnvironmentOptions(string basePath, string tempPath, string journalPath)
            {
                _basePath = Path.GetFullPath(basePath);
                _tempPath = !string.IsNullOrEmpty(tempPath) ? Path.GetFullPath(tempPath) : _basePath;
                _journalPath = !string.IsNullOrEmpty(journalPath) ? Path.GetFullPath(journalPath) : _basePath;

                if (Directory.Exists(_basePath) == false)
                    Directory.CreateDirectory(_basePath);

                if (_basePath != tempPath && Directory.Exists(_tempPath) == false)
                    Directory.CreateDirectory(_tempPath);

                if (_journalPath != tempPath && Directory.Exists(_journalPath) == false)
                    Directory.CreateDirectory(_journalPath);

                _dataPager = new Lazy<IVirtualPager>(() =>
                {
                    FilePath = Path.Combine(_basePath, Constants.DatabaseFilename);
                    if (RunningOnPosix)
                        return new PosixMemoryMapPager(PageSize, FilePath, InitialFileSize);
                    return new Win32MemoryMapPager(PageSize, FilePath, InitialFileSize);
                });
            }

            public string FilePath { get; private set; }

            public override IVirtualPager DataPager
            {
                get
                {
                    return _dataPager.Value;
                }
            }

            public override string BasePath
            {
                get { return _basePath; }
            }

            public string TempPath
            {
                get { return _tempPath; }
            }

            public override IVirtualPager OpenPager(string filename)
            {
                if (RunningOnPosix)
                    return new PosixMemoryMapPager(PageSize, filename);

                return new Win32MemoryMapPager(PageSize,filename);
            }


            public override IJournalWriter CreateJournalWriter(long journalNumber, long journalSize)
            {
                var name = JournalName(journalNumber);
                var path = Path.Combine(_journalPath, name);
                var result = _journals.GetOrAdd(name, _ => new Lazy<IJournalWriter>(() =>
                {
                    if (RunningOnPosix)
                        return new PosixJournalWriter(this,path, journalSize);

                    return new Win32FileJournalWriter(this,path, journalSize);
                }));

                if (result.Value.Disposed)
                {
                    var newWriter = new Lazy<IJournalWriter>(() =>
                    {
                        if (RunningOnPosix)
                            return new PosixJournalWriter(this,path, journalSize);

                        return new Win32FileJournalWriter(this, path, journalSize);
                    });
                    if (_journals.TryUpdate(name, newWriter, result) == false)
                        throw new InvalidOperationException("Could not update journal pager");
                    result = newWriter;
                }

                return result.Value;
            }

            public override void Dispose()
            {
                if (Disposed)
                    return;
                Disposed = true;
                if (_dataPager.IsValueCreated)
                    _dataPager.Value.Dispose();
                foreach (var journal in _journals)
                {
                    if (journal.Value.IsValueCreated)
                        journal.Value.Value.Dispose();
                }
            }

            public override bool TryDeleteJournal(long number)
            {
                var name = JournalName(number);

                Lazy<IJournalWriter> lazy;
                if (_journals.TryRemove(name, out lazy) && lazy.IsValueCreated)
                    lazy.Value.Dispose();

                var file = Path.Combine(_journalPath, name);
                if (File.Exists(file) == false)
                    return false;
                File.Delete(file);
                return true;
            }

            public unsafe override bool ReadHeader(string filename, FileHeader* header)
            {
                var path = Path.Combine(_basePath, filename);
                if (File.Exists(path) == false)
                {
                    return false;
                }
                if (RunningOnPosix)
                    return PosixHelper.TryReadFileHeader(header, path);
                return Win32Helper.TryReadFileHeader(header, path);
            }


            public override unsafe void WriteHeader(string filename, FileHeader* header)
            {
                var path = Path.Combine(_basePath, filename);
                if (RunningOnPosix)
                    PosixHelper.WriteFileHeader(header, path);
                else
                    Win32Helper.WriteFileHeader(header, path);
            }


            public override IVirtualPager CreateScratchPager(string name)
            {
                var scratchFile = Path.Combine(_tempPath, name);
                if (File.Exists(scratchFile))
                    File.Delete(scratchFile);

                if (RunningOnPosix)
                {
                    return new PosixMemoryMapPager(PageSize,scratchFile, InitialFileSize)
                    {
                        DeleteOnClose = true
                    };
                }
                return new Win32MemoryMapPager(PageSize, scratchFile, InitialFileSize, (Win32NativeFileAttributes.DeleteOnClose | Win32NativeFileAttributes.Temporary));
            }

            public override IVirtualPager OpenJournalPager(long journalNumber)
            {
                var name = JournalName(journalNumber);
                var path = Path.Combine(_journalPath, name);
                if (File.Exists(path) == false)
                    throw new InvalidOperationException("No such journal " + path);
                if (RunningOnPosix)
                    return new PosixMemoryMapPager(PageSize, path);
                var win32MemoryMapPager = new Win32MemoryMapPager(PageSize, path, access: Win32NativeFileAccess.GenericRead, 
                    options:Win32NativeFileAttributes.SequentialScan);
                win32MemoryMapPager.TryPrefetchingWholeFile();
                return win32MemoryMapPager;
            }
        }

        public class PureMemoryStorageEnvironmentOptions : StorageEnvironmentOptions
        {
            private static int _counter;

            private readonly IVirtualPager _dataPager;

            private readonly Dictionary<string, IJournalWriter> _logs =
                new Dictionary<string, IJournalWriter>(StringComparer.OrdinalIgnoreCase);

            private readonly Dictionary<string, IntPtr> _headers =
                new Dictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);
            private int _instanceId;

            private string tempPath { get; set; }

            public PureMemoryStorageEnvironmentOptions(string configTempPath)
            {
                tempPath = configTempPath;
                _instanceId = Interlocked.Increment(ref _counter);
                var guid = Guid.NewGuid();
                var filename = $"ravendb-{Process.GetCurrentProcess().Id}-{_instanceId}-data.pager-{guid}";

                if (RunningOnPosix)
                {
                    _dataPager = new PosixTempMemoryMapPager(PageSize, Path.Combine(tempPath, filename), InitialFileSize);
                }
                else
                {
                    _dataPager = new Win32MemoryMapPager(PageSize, Path.Combine(tempPath, filename), InitialFileSize,
                        Win32NativeFileAttributes.RandomAccess | Win32NativeFileAttributes.DeleteOnClose | Win32NativeFileAttributes.Temporary);
                }
            }

            public override IVirtualPager DataPager
            {
                get { return _dataPager; }
            }

            public override string BasePath
            {
                get { return ":memory:"; }
            }

            public override IJournalWriter CreateJournalWriter(long journalNumber, long journalSize)
            {
                var name = JournalName(journalNumber);
                IJournalWriter value;
                if (_logs.TryGetValue(name, out value))
                    return value;
                value = new PureMemoryJournalWriter(this, journalSize);
                _logs[name] = value;
                return value;
            }

            public override void Dispose()
            {
                if (Disposed)
                    return;
                Disposed = true;
                _dataPager.Dispose();
                foreach (var virtualPager in _logs)
                {
                    virtualPager.Value.Dispose();
                }

                foreach (var headerSpace in _headers)
                {
                    Marshal.FreeHGlobal(headerSpace.Value);
                }
                _headers.Clear();
            }

            public override bool TryDeleteJournal(long number)
            {
                var name = JournalName(number);
                IJournalWriter value;
                if (_logs.TryGetValue(name, out value) == false)
                    return false;
                _logs.Remove(name);
                value.Dispose();
                return true;
            }

            public unsafe override bool ReadHeader(string filename, FileHeader* header)
            {
                if(Disposed)
                    throw new ObjectDisposedException(nameof(PureMemoryStorageEnvironmentOptions));
                IntPtr ptr;
                if (_headers.TryGetValue(filename, out ptr) == false)
                {
                    return false;
                }
                *header = *((FileHeader*)ptr);
                return true;
            }

            public override unsafe void WriteHeader(string filename, FileHeader* header)
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(PureMemoryStorageEnvironmentOptions));

                IntPtr ptr;
                if (_headers.TryGetValue(filename, out ptr) == false)
                {
                    ptr = Marshal.AllocHGlobal(sizeof(FileHeader));
                    _headers[filename] = ptr;
                }
                Memory.Copy((byte*)ptr, (byte*)header, sizeof(FileHeader));
            }

            public override IVirtualPager CreateScratchPager(string name)
            {
                var guid = Guid.NewGuid();
                var filename = $"ravendb-{Process.GetCurrentProcess().Id}-{_instanceId}-{name}-{guid}";

                if (RunningOnPosix)
                    return new PosixTempMemoryMapPager(PageSize, Path.Combine(tempPath, filename), InitialFileSize);


                return new Win32MemoryMapPager(PageSize, Path.Combine(tempPath, filename), InitialFileSize,
                        Win32NativeFileAttributes.RandomAccess | Win32NativeFileAttributes.DeleteOnClose | Win32NativeFileAttributes.Temporary);
            }

            public override IVirtualPager OpenPager(string filename)
            {
                if (RunningOnPosix)
                    return new PosixMemoryMapPager(PageSize, filename);
                return new Win32MemoryMapPager(PageSize, filename);
            }

            public override IVirtualPager OpenJournalPager(long journalNumber)
            {
                var name = JournalName(journalNumber);
                IJournalWriter value;
                if (_logs.TryGetValue(name, out value))
                    return value.CreatePager();
                throw new InvalidOperationException("No such journal " + journalNumber);
            }
        }

        public static string JournalName(long number)
        {
            return string.Format("{0:D19}.journal", number);
        }

        public static string JournalRecoveryName(long number)
        {
            return string.Format("{0:D19}.recovery", number);
        }

        public static string ScratchBufferName(long number)
        {
            return string.Format("scratch.{0:D10}.buffers", number);
        }

#pragma warning disable CC0029 // Disposables Should Call Suppress Finalize
        public abstract void Dispose();
#pragma warning restore CC0029 // Disposables Should Call Suppress Finalize

        public abstract bool TryDeleteJournal(long number);

        public unsafe abstract bool ReadHeader(string filename, FileHeader* header);

        public unsafe abstract void WriteHeader(string filename, FileHeader* header);

        public abstract IVirtualPager CreateScratchPager(string name);

        public abstract IVirtualPager OpenJournalPager(long journalNumber);

        public abstract IVirtualPager OpenPager(string filename);

#if DNXCORE50
        public static bool RunningOnPosix
            => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
               RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#else
        public static bool RunningOnPosix => false;
#endif
    }
}
