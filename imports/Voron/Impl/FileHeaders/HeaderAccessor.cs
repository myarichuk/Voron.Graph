﻿// -----------------------------------------------------------------------
//  <copyright file="HeaderAccessor.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using Sparrow;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Voron.Global;

namespace Voron.Impl.FileHeaders
{
    public unsafe delegate void ModifyHeaderAction(FileHeader* ptr);

    public unsafe delegate T GetDataFromHeaderAction<T>(FileHeader* ptr);

    public unsafe class HeaderAccessor : IDisposable
    {
        private readonly StorageEnvironment _env;

        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
        private long _revision;

        private FileHeader* _theHeader;
        private IntPtr _headerPtr;

        internal static string[] HeaderFileNames = { "headers.one", "headers.two" };

        public HeaderAccessor(StorageEnvironment env)
        {
            _env = env;

            _headerPtr = Marshal.AllocHGlobal(sizeof(FileHeader));
            _theHeader = (FileHeader*)_headerPtr.ToPointer();
        }

        public bool Initialize()
        {
            _locker.EnterWriteLock();
            try
            {
                if (_theHeader == null)
                    throw new ObjectDisposedException("Cannot access the header after it was disposed");

                var headers = stackalloc FileHeader[2];
                var f1 = &headers[0];
                var f2 = &headers[1];
                var hasHeader1 = _env.Options.ReadHeader(HeaderFileNames[0], f1);
                var hasHeader2 = _env.Options.ReadHeader(HeaderFileNames[1], f2);
                if (hasHeader1 == false && hasHeader2 == false)
                {
                    // new 
                    FillInEmptyHeader(f1);
                    FillInEmptyHeader(f2);
                    _env.Options.WriteHeader(HeaderFileNames[0], f1);
                    _env.Options.WriteHeader(HeaderFileNames[1], f2);

                    Memory.Copy((byte*)_theHeader, (byte*)f1, sizeof(FileHeader));
                    return true; // new
                }

                if (f1->MagicMarker != Constants.MagicMarker && f2->MagicMarker != Constants.MagicMarker)
                    throw new InvalidDataException("None of the header files start with the magic marker, probably not db files opr");

                // if one of the files is corrupted, but the other isn't, restore to the valid file
                if (f1->MagicMarker != Constants.MagicMarker)
                {
                    *f1 = *f2;
                }
                if (f2->MagicMarker != Constants.MagicMarker)
                {
                    *f2 = *f1;
                }

                if (f1->Version != Constants.CurrentVersion)
                    throw new InvalidDataException("This is a db file for version " + f1->Version + ", which is not compatible with the current version " + Constants.CurrentVersion + Environment.NewLine +
                        "Error at " + _env.Options.BasePath);

                if (f1->TransactionId < 0)
                    throw new InvalidDataException("The transaction number cannot be negative");


                if (f1->HeaderRevision > f2->HeaderRevision)
                {
                    Memory.Copy((byte*)_theHeader, (byte*)f1, sizeof(FileHeader));
                }
                else
                {
                    Memory.Copy((byte*)_theHeader, (byte*)f2, sizeof(FileHeader));
                }
                _revision = _theHeader->HeaderRevision;

                if (_theHeader->PageSize != _env.Options.PageSize)
                {
                    var message = string.Format("PageSize mismatch, configured to be {0:#,#} but was {1:#,#}, using the actual value in the file {1:#,#}",
                        _env.Options.PageSize, _theHeader->PageSize);
                    _env.Options.InvokeRecoveryError(this, message, null);
                    _env.Options.PageSize = _theHeader->PageSize;
                }

                return false;
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }


        public FileHeader CopyHeader()
        {
            _locker.EnterReadLock();
            try
            {
                if (_theHeader == null)
                    throw new ObjectDisposedException("Cannot access the header after it was disposed");
                return *_theHeader;
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }


        public T Get<T>(GetDataFromHeaderAction<T> action)
        {
            _locker.EnterReadLock();
            try
            {
                if (_theHeader == null)
                    throw new ObjectDisposedException("Cannot access the header after it was disposed");

                return action(_theHeader);
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        public void Modify(ModifyHeaderAction modifyAction)
        {
            _locker.EnterWriteLock();
            try
            {
                if (_theHeader == null)
                    throw new ObjectDisposedException("Cannot access the header after it was disposed");


                modifyAction(_theHeader);

                _revision++;
                _theHeader->HeaderRevision = _revision;

                var file = HeaderFileNames[_revision & 1];

                _env.Options.WriteHeader(file, _theHeader);

            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        private void FillInEmptyHeader(FileHeader* header)
        {
            header->MagicMarker = Constants.MagicMarker;
            header->Version = Constants.CurrentVersion;
            header->HeaderRevision = -1;
            header->TransactionId = 0;
            header->LastPageNumber = 1;
            header->Root.RootPageNumber = -1;
            header->Journal.CurrentJournal = -1;
            header->Journal.JournalFilesCount = 0;
            header->Journal.LastSyncedJournal = -1;
            header->Journal.LastSyncedTransactionId = -1;
            header->IncrementalBackup.LastBackedUpJournal = -1;
            header->IncrementalBackup.LastBackedUpJournalPage = -1;
            header->IncrementalBackup.LastCreatedJournal = -1;
            header->PageSize = _env.Options.PageSize;
        }

        public void Dispose()
        {
            _locker.EnterWriteLock();
            try
            {
                if (_headerPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_headerPtr);
                    _headerPtr = IntPtr.Zero;
                    _theHeader = null;
                }
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }
    }
}