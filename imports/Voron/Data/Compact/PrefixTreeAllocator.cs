﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Voron.Data.Fixed;
using Voron.Impl;

namespace Voron.Data.Compact
{
    public unsafe class PrefixTreeAllocator
    {
        private readonly LowLevelTransaction _tx;
        private readonly FixedSizeTree _freeSpaceTree;
        private readonly PageLocator _pageLocator;
        private readonly long _chunkPageSize;

        private PageHandlePtr _lastPage;

        public PrefixTreeAllocator(Transaction tx, PageLocator pageLocator, FixedSizeTree freeSpaceTree)
        {
            _tx = tx.LowLevelTransaction;
            _pageLocator = pageLocator;
            _freeSpaceTree = freeSpaceTree;
            _chunkPageSize = _tx.DataPager.PageSize;
        }  

        public bool AreNodesInSameChunk(long aPtr, long bPtr)
        {
            long naChunkIdx = aPtr / _chunkPageSize;
            long nbChunkIdx = bPtr / _chunkPageSize;

            return naChunkIdx == nbChunkIdx;
        }

        public long AllocateNode(long parentPtr, out PrefixTree.Node* node)
        {
            if (_tx.Flags == TransactionFlags.Read)
                throw new InvalidOperationException("Cannot allocate a node in a read transaction.");

            long nodePtr = PrefixTree.Constants.InvalidNodeName;
            if (_lastPage.IsValid && _lastPage.IsWritable)
            {
                if ( TryAllocateNodeInPage(_lastPage.Value.ToPrefixTreePage(), out nodePtr, out node) )
                    return nodePtr;
            }                

            return AllocateOnAnyChunk(out node);
        }

        public bool TryAllocateNodeInPage( PrefixTreePage chunkPage, out long nodePtr, out PrefixTree.Node* node)
        {
            // We will try to allocate from the chunk free space.
            int idx = chunkPage.FreeSpace.FindLeadingOne();
            if (idx < 0) // Check if we have space available. 
            {
                // We dont have any spot available, therefore we remove the page (it is full) and fail. 
                _freeSpaceTree.Delete(chunkPage.PageNumber);
                nodePtr = PrefixTree.Constants.InvalidNodeName;
                node = null;

                return false;
            }

            chunkPage.FreeSpace.Set(idx, false);
            Debug.Assert(chunkPage.FreeSpace.Get(idx) == false);
            Debug.Assert(chunkPage.FreeSpace.FindLeadingOne() != idx);

            // Convert relative node position to the real memory address on disk.
            nodePtr = chunkPage.GetDiskPointer(idx);
            node = chunkPage.GetNodePtrByIndex(idx);
            return true;
        }
   
        private long AllocateOnAnyChunk(out PrefixTree.Node* node)
        {
            List<long> chunksAlreadyFull = null;

            try
            {
                Page page;
                PrefixTreePage chunkPage;
                using (var it = _freeSpaceTree.Iterate())
                {                   
                    while (it.MoveNext())
                    {
                        var chunkPageNumber = it.CurrentKey;

                        chunkPage = _pageLocator.GetReadOnlyPage(chunkPageNumber).ToPrefixTreePage();
                        
                        // We will try to allocate from the chunk free space.
                        int idx = chunkPage.FreeSpace.FindLeadingOne();
                        if (idx < 0) // Check if we have space available. 
                        {
                            // We dont have any spot available, therefore we remove the page (it is full) and fail. 
                            if (chunksAlreadyFull == null)
                                chunksAlreadyFull = new List<long>();

                            chunksAlreadyFull.Add(chunkPageNumber);
                            continue;
                        }

                        // We can allocate, so we open the page for writing (we will pay the modify now and cache it at the transaction level). 
                        page = _pageLocator.GetWritablePage(chunkPageNumber);
                        _lastPage = new PageHandlePtr(page, true);

                        chunkPage = page.ToPrefixTreePage();
                        chunkPage.FreeSpace.Set(idx, false);

                        // Convert relative node position to the real memory address on disk.
                        node = chunkPage.GetNodePtrByIndex(idx);
                        return chunkPage.GetDiskPointer(idx);
                    }
                }

                // We dont have any place from the free space. Therefore we will allocate. 
                page = _tx.AllocatePage(1);
                chunkPage = page.ToPrefixTreePage();
                chunkPage.Initialize();
                _lastPage = new PageHandlePtr(page, true);

                _freeSpaceTree.Add(page.PageNumber);
                chunkPage.FreeSpace.Set(0, false);
               

                // Convert relative node position to the real memory address on disk.
                node = chunkPage.GetNodePtrByIndex(0);
                return chunkPage.GetDiskPointer(0);
            }
            finally
            {
                if ( chunksAlreadyFull != null )
                {
                    foreach (long chunk in chunksAlreadyFull)
                        _freeSpaceTree.Delete(chunk);
                }
            }
        }

        public void DeallocateNode(long nodePtr)
        {
            long chunkPageNumber = nodePtr / _chunkPageSize;

            if (chunkPageNumber != _lastPage.PageNumber)
            {
                // We can allocate, so we open the page for writing (we will pay the modify now and cache it at the transaction level). 
                Page page = _pageLocator.GetWritablePage(chunkPageNumber);
                _lastPage = new PageHandlePtr(page, true);
            }

            PrefixTreePage chunkPage = _lastPage.Value.ToPrefixTreePage();

            // We mark the node as unused.
            long nodeIdx = chunkPage.GetIndexFromDiskPointer(nodePtr);
            chunkPage.FreeSpace.Set((int)nodeIdx, true);

            if (!_freeSpaceTree.Contains(chunkPageNumber))
                _freeSpaceTree.Add(chunkPageNumber);
        }
    }
}
