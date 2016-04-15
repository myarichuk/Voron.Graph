﻿using System;
using System.Runtime.InteropServices;

namespace Voron.Data.Compact
{
    /// <summary>
    /// The Prefix Tree Root Header.
    /// </summary>    
    /// <remarks>This header extends the <see cref="RootHeader"/> structure.</remarks>    
    //TODO: Change this when we are ready to go.
    //[StructLayout(LayoutKind.Explicit, Pack = 1)]
    [StructLayout(LayoutKind.Sequential)]
    public struct PrefixTreeRootHeader
    {
        public RootObjectType RootObjectType;

        /// <summary>
        /// The root node name for the tree. 
        /// </summary>
        public long RootNodeName;

        /// <summary>
        /// This is the amount of elements already stored in the tree. 
        /// </summary>
        public long Items;

        /// <summary>
        /// The link to the internal free space table (a fixed size b-tree) that will hold the pages with free space for nodes.
        /// </summary>
        // TODO: Add the leaf one too afterwards.
        public long FreeSpace;

        /// <summary>
        /// The head node pointer for the tree. 
        /// </summary>
        public PrefixTree.Leaf Head;

        /// <summary>
        /// The tail node pointer for the tree. 
        /// </summary>
        public PrefixTree.Leaf Tail;

        /// <summary>
        /// The table header page for the tree.
        /// </summary>
        public PrefixTreeTableHeader Table;

        public void Initialize()
        {
            this.RootObjectType = RootObjectType.PrefixTree;
        }
    }
}
