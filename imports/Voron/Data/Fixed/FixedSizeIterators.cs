﻿// -----------------------------------------------------------------------
//  <copyright file="Iterators.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System;
using Voron.Impl;
using Voron.Impl.FileHeaders;

namespace Voron.Data.Fixed
{
    public unsafe partial class FixedSizeTree
    {
        public interface IFixedSizeIterator : IDisposable
        {
	        bool SeekToLast();
            bool Seek(long key);
            long CurrentKey { get; }
            Slice Value { get; }
            bool MoveNext();
            bool MovePrev();

			/// <summary>
			/// Deletes the current key/value pair and returns true if there is 
			/// another key after it
			/// </summary>
			bool DeleteCurrentAndMoveNext();

            ValueReader CreateReaderForCurrent();

	        bool Skip(int count);
        }

        public class NullIterator : IFixedSizeIterator
        {
	        public bool SeekToLast()
	        {
		        return false;
	        }

	        public bool Seek(long key)
            {
                return false;
            }

            public long CurrentKey { get { throw new InvalidOperationException("Invalid position, cannot read past end of tree"); } }
            public Slice Value { get { throw new InvalidOperationException("Invalid position, cannot read past end of tree"); } }
            public bool MoveNext()
            {
                return false;
            }

            public bool MovePrev()
            {
                return false;
            }

            public bool DeleteCurrentAndMoveNext()
	        {
		        throw new InvalidOperationException("Invalid position, cannot read past end of tree");
	        }

	        public void Dispose()
            {
            }

            public ValueReader CreateReaderForCurrent()
            {
                throw new InvalidOperationException("No current page");
            }

	        public bool Skip(int count)
	        {
				return false;
	        }
        }

        public class EmbeddedIterator : IFixedSizeIterator
        {
            private readonly FixedSizeTree _fst;
            private int _pos;
            private FixedSizeTreeHeader.Embedded* _header;
            private byte* _dataStart;

            public EmbeddedIterator(FixedSizeTree fst)
            {
                _fst = fst;
                var ptr = _fst._parent.DirectRead(_fst._treeName);
                _header = (FixedSizeTreeHeader.Embedded*)ptr;
                _dataStart = ptr + sizeof(FixedSizeTreeHeader.Embedded);
            }

	        public bool SeekToLast()
	        {
				if (_header == null)
					return false;
		        _pos = _header->NumberOfEntries - 1;
		        return true;
	        }

			public bool Seek(long key)
            {
	            if (_header == null)
		            return false;
	            _pos = _fst.BinarySearch(_dataStart, _header->NumberOfEntries, key, _fst._entrySize);
				if (_fst._lastMatch > 0)
					_pos++; // We didn't find the key.
                return _pos != _header->NumberOfEntries;
            }

            public long CurrentKey
            {
                get
                {
                    if (_pos == _header->NumberOfEntries)
                        throw new InvalidOperationException("Invalid position, cannot read past end of tree");
                    return _fst.KeyFor(_dataStart, _pos, _fst._entrySize);
                }
            }

            public Slice Value
            {
                get
                {
                    if (_pos == _header->NumberOfEntries)
                        throw new InvalidOperationException("Invalid position, cannot read past end of tree");

                    return new Slice(_dataStart + (_pos * _fst._entrySize) + sizeof(long), _fst._valSize);
                }
            }

            public bool MovePrev()
            {
                return --_pos >= 0;
            }

            public bool MoveNext()
            {
                return ++_pos < _header->NumberOfEntries;
            }

	        public bool DeleteCurrentAndMoveNext()
	        {
				var currentKey = CurrentKey;
		        _fst.RemoveEmbeddedEntry(currentKey);
				var ptr = _fst._parent.DirectRead(_fst._treeName);
		        if (ptr == null)
			        return false;
		        _header = (FixedSizeTreeHeader.Embedded*)ptr;
				_dataStart = ptr + sizeof(FixedSizeTreeHeader.Embedded);
				return Seek(currentKey);    
	        }

	        public ValueReader CreateReaderForCurrent()
            {
                return new ValueReader(_dataStart + (_pos * _fst._entrySize) + sizeof(long), _fst._valSize);
            }

	        public bool Skip(int count)
	        {
				if (count != 0)
					_pos += count;

				return _pos < _header->NumberOfEntries;
	        }

	        public void Dispose()
            {
            }
        }

        public class LargeIterator : IFixedSizeIterator
        {
            private readonly FixedSizeTree _parent;
            private FixedSizeTreePage _currentPage;

            public LargeIterator(FixedSizeTree parent)
            {
                _parent = parent;
            }

            public void Dispose()
            {
                
            }

            public bool Seek(long key)
            {
                _currentPage = _parent.FindPageFor(key);
	            return _currentPage.LastMatch <= 0 || MoveNext();
            }

	        public bool SeekToLast()
	        {
				_currentPage = _parent.FindPageFor(long.MaxValue);
		        return true;
	        }

			public long CurrentKey
            {
                get
                {
                    if (_currentPage == null)
                        throw new InvalidOperationException("No current page was set");

                    return _parent.KeyFor(_currentPage,_currentPage.LastSearchPosition);
                }
            }

            public Slice Value
            {
                get
                {
                    if (_currentPage == null)
                        throw new InvalidOperationException("No current page was set");

                    return new Slice(_currentPage.Pointer + _currentPage.StartPosition + (_parent._entrySize * _currentPage.LastSearchPosition) + sizeof(long), _parent._valSize);
                }
            }

            public bool MoveNext()
            {
                if (_currentPage == null)
                    throw new InvalidOperationException("No current page was set");

                while (_currentPage != null)
                {
                    _currentPage.LastSearchPosition++;
                    if (_currentPage.LastSearchPosition < _currentPage.NumberOfEntries)
                    {
                        // run out of entries, need to select the next page...
                        while (_currentPage.IsBranch)
                        {
                            _parent._cursor.Push(_currentPage);
                            var childParentNumber = _parent.PageValueFor(_currentPage,_currentPage.LastSearchPosition);
                            _currentPage = _parent._tx.GetReadOnlyFixedSizeTreePage(childParentNumber);

                            _currentPage.LastSearchPosition = 0;
                        }
                        return true;// there is another entry in this page
                    }
                    if (_parent._cursor.Count == 0)
                        break;
                    _currentPage = _parent._cursor.Pop();
                }
                _currentPage = null;

                return false;
            }

            public bool MovePrev()
            {
                if (_currentPage == null)
                    throw new InvalidOperationException("No current page was set");

                while (_currentPage != null)
                {
                    _currentPage.LastSearchPosition--;
                    if (_currentPage.LastSearchPosition >= 0)
                    {
                        // run out of entries, need to select the next page...
                        while (_currentPage.IsBranch)
                        {
                            _parent._cursor.Push(_currentPage);
                            var childParentNumber = _parent.PageValueFor(_currentPage, _currentPage.LastSearchPosition);
                            _currentPage = _parent._tx.GetReadOnlyFixedSizeTreePage(childParentNumber);

                            _currentPage.LastSearchPosition = _currentPage.NumberOfEntries - 1;
                        }
                        return true;// there is another entry in this page
                    }
                    if (_parent._cursor.Count == 0)
                        break;
                    _currentPage = _parent._cursor.Pop();
                }
                _currentPage = null;

                return false;
            }


            /// <summary>
            /// Deletes the current key/value pair and returns true if there is 
            /// another key after it
            /// </summary>
            public bool DeleteCurrentAndMoveNext()
	        {
				var currentKey = CurrentKey;
				
				_parent.RemoveLargeEntry(currentKey);
				if (_parent._type == RootObjectType.FixedSizeTree)
				{
					return Seek(currentKey);
				}
				return true;
	        }

	        public ValueReader CreateReaderForCurrent()
            {
                if (_currentPage == null)
                    throw new InvalidOperationException("No current page was set");

                return new ValueReader(_currentPage.Pointer + _currentPage.StartPosition + (_parent._entrySize * _currentPage.LastSearchPosition) + sizeof(long), _parent._valSize);
            }

	        public bool Skip(int count)
	        {
				if (count != 0)
				{
					for (int i = 0; i < Math.Abs(count); i++)
					{
						if (!MoveNext())
							break;
					}
				}

				var seek = _currentPage != null && _currentPage.LastSearchPosition != _currentPage.NumberOfEntries;
				if (seek == false)
					_currentPage = null;
				return seek;
	        }
        }
    }
}