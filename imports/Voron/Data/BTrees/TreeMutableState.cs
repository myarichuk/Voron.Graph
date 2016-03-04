﻿using System;
using System.Diagnostics;
using Voron.Impl;
using Voron.Impl.FileHeaders;

namespace Voron.Data.BTrees
{
    public unsafe class TreeMutableState
    {
        private readonly LowLevelTransaction _tx;
	    public long BranchPages;
        public long LeafPages;
        public long OverflowPages;
        public int Depth;
        public long PageCount;
        public long NumberOfEntries;
	    public TreeFlags Flags;

        public long RootPageNumber;
        private bool _isModified;

        public bool InWriteTransaction;

        public TreeMutableState(LowLevelTransaction tx)
	    {
		    _tx = tx;
	    }

	    public bool IsModified
        {
            get { return _isModified; }
            set
            {
                if (InWriteTransaction == false)
                    throw new InvalidOperationException("Invalid operation outside of a write transaction");
                _isModified = value;
            }
        }

	    public void CopyTo(TreeRootHeader* header)
	    {
	        header->RootObjectType = RootObjectType.VariableSizeTree;
			header->Flags = Flags;
            header->BranchPages = BranchPages;
            header->Depth = Depth;
            header->LeafPages = LeafPages;
            header->OverflowPages = OverflowPages;
            header->PageCount = PageCount;
            header->NumberOfEntries = NumberOfEntries;
            header->RootPageNumber = RootPageNumber;
        }

        public TreeMutableState Clone()
        {
            return new TreeMutableState(_tx)
                {
                    BranchPages = BranchPages,
                    Depth = Depth,
                    NumberOfEntries = NumberOfEntries,
                    LeafPages = LeafPages,
                    OverflowPages = OverflowPages,
                    PageCount = PageCount,
					Flags = Flags,
                    RootPageNumber = RootPageNumber,
                };
        }

		public void RecordNewPage(TreePage p, int num)
		{
			PageCount += num;

			if (p.IsBranch)
			{
				BranchPages++;
			}
			else if (p.IsLeaf)
			{
				LeafPages++;
			}
			else if (p.IsOverflow)
			{
				OverflowPages += num;
			}
		}

		public void RecordFreedPage(TreePage p, int num)
		{
			PageCount -= num;
			Debug.Assert(PageCount >= 0);

			if (p.IsBranch)
			{
				BranchPages--;
				Debug.Assert(BranchPages >= 0);
			}
			else if (p.IsLeaf)
			{
				LeafPages--;
				Debug.Assert(LeafPages >= 0);
			}
			else if (p.IsOverflow)
			{
				OverflowPages -= num;
				Debug.Assert(OverflowPages >= 0);
			}
		}

        public override string ToString()
        {
            return string.Format(@" Pages: {1:#,#}, Entries: {2:#,#}
    Depth: {0}, FixedTreeFlags: {3}
    Root Page: {4}
    Leafs: {5:#,#} Overflow: {6:#,#} Branches: {7:#,#}
    Size: {8:F2} Mb", Depth, PageCount, NumberOfEntries, Flags, RootPageNumber, LeafPages, OverflowPages, BranchPages, ((float)(PageCount * _tx.DataPager.PageSize) / (1024 * 1024)));
        }
    }
}
