using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Sparrow.Binary;
using Sparrow.Server;
using Voron.Data.Fixed;
using Voron.Data.RawData;
using Voron.Data.Tables;
using Voron.Impl;

namespace Voron.Graph
{
    public unsafe class GraphEnvironment : IDisposable
    {
        private readonly StorageEnvironment _env;

        private static readonly Slice VerticesSlice;
        private static readonly Slice EdgesSlice;

        private static readonly Slice EtagSlice;
        private static readonly Slice ToFromSlice;
        private static readonly Slice ToSlice;
        private static readonly Slice FromSlice;

        private static readonly Slice ChangeVectorSlice;

        private static readonly Slice VertexEtagsSlice;
        private static readonly Slice EdgeEtagsSlice;

        private long _nextVertexEtag;
        private long _nextEdgeEtag;

        public enum VertexTable
        {
            Id = 0,
            Etag = 1,
            ChangeVector = 2
        }

        public enum EdgeTable
        {
            Id = 0,
            From = 1,
            To = 2,
            Etag = 3,
            ChangeVector = 4,
            Data = 5
        }

        public static readonly TableSchema VertexSchema = new TableSchema();
        public static readonly TableSchema EdgeSchema = new TableSchema();


        static GraphEnvironment()
        {
            ExtractLibrvnAssemblies();

            using (StorageEnvironment.GetStaticContext(out var ctx))
            {
                Slice.From(ctx, "VerticesSlice", ByteStringType.Immutable, out VerticesSlice);
                Slice.From(ctx, "EdgesSlice", ByteStringType.Immutable, out EdgesSlice);

                Slice.From(ctx, "Etag", ByteStringType.Immutable, out EtagSlice);
                Slice.From(ctx, "ToFromSlice", ByteStringType.Immutable, out ToFromSlice);
                Slice.From(ctx, "ToSlice", ByteStringType.Immutable, out ToSlice);
                Slice.From(ctx, "FromSlice", ByteStringType.Immutable, out FromSlice);
                Slice.From(ctx, "ChangeVectorSlice", ByteStringType.Immutable, out ChangeVectorSlice);

                Slice.From(ctx, "VertexEtagsSlice", ByteStringType.Immutable, out VertexEtagsSlice);
                Slice.From(ctx, "EdgeEtagsSlice", ByteStringType.Immutable, out EdgeEtagsSlice);
            }

            VertexSchema.DefineKey(new TableSchema.SchemaIndexDef
            {
                StartIndex = (int) VertexTable.Id,
                Count = 1,
                IsGlobal = false,
                Name = VerticesSlice
            });

            VertexSchema.DefineFixedSizeIndex(new TableSchema.FixedSizeSchemaIndexDef
            {
                StartIndex = (int) VertexTable.Etag,
                IsGlobal = false,
                Name = EtagSlice
            });

            EdgeSchema.DefineKey(new TableSchema.SchemaIndexDef
            {
                StartIndex = (int) EdgeTable.Id,
                Count = 1,
                IsGlobal = false,
                Name = EdgesSlice
            });

            EdgeSchema.DefineFixedSizeIndex(new TableSchema.FixedSizeSchemaIndexDef
            {
                StartIndex = (int) EdgeTable.Etag,
                IsGlobal = false,
                Name = EtagSlice
            });

            EdgeSchema.DefineIndex(new TableSchema.SchemaIndexDef
            {
                Count = 2,
                StartIndex = (int) EdgeTable.From,
                Name = ToFromSlice
            });

            EdgeSchema.DefineFixedSizeIndex(new TableSchema.FixedSizeSchemaIndexDef
            {
                StartIndex = (int) EdgeTable.From,
                IsGlobal = false,
                Name = FromSlice
            });

            EdgeSchema.DefineFixedSizeIndex(new TableSchema.FixedSizeSchemaIndexDef
            {
                StartIndex = (int) EdgeTable.To,
                IsGlobal = false,
                Name = ToSlice
            });
        }

        #region Extract embedded librvn assemblies

        private static void ExtractLibrvnAssemblies()
        {
            var dirName = AppDomain.CurrentDomain.BaseDirectory;

            var embeddedDlls = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var dllPaths = new List<string>();
            foreach (var embeddedDll in embeddedDlls)
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedDll))
                {
                    try
                    {
                        var dllPath = Path.Combine(dirName,
                            embeddedDll.Replace("Voron.Graph.librvnpal.", string.Empty));
                        dllPaths.Add(dllPath);
                        using (Stream fileStream = File.Create(dllPath))
                        {
                            const int sz = 4096;
                            var buf = new byte[sz];
                            while (true)
                            {
                                int nRead = stream.Read(buf, 0, sz);
                                if (nRead < 1)
                                    break;
                                fileStream.Write(buf, 0, nRead);
                            }

                            fileStream.Flush();
                        }
                    }
                    catch
                    {
                    }
                }
            }

            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                foreach (var dll in dllPaths)
                {
                    try
                    {
                        File.Delete(dll);
                    }
                    catch
                    {
                    }
                }
            };
        }

        #endregion

        public GraphEnvironment(bool inMemory = false)
            : this(inMemory
                ? StorageEnvironmentOptions.CreateMemoryOnly()
                : StorageEnvironmentOptions.ForPath(AppDomain.CurrentDomain.BaseDirectory))
        {
        }

        public GraphEnvironment(StorageEnvironmentOptions environmentOptions)
            : this(new StorageEnvironment(environmentOptions))
        {
        }

        public GraphEnvironment(StorageEnvironment env)
        {
            _env = env;
            Initialize();
        }

        private Table GetVerticesTable(Transaction tx)
        {
            var table = tx.OpenTable(VertexSchema, VerticesSlice);
            table.DataMoved += OnVertexDataMoved;
            return table;
        }

        private Table GetEdgesTable(Transaction tx) => tx.OpenTable(EdgeSchema, EdgesSlice);


        private void Initialize()
        {
            using (var tx = _env.WriteTransaction())
            {
                NewPageAllocator.MaybePrefetchSections(
                    tx.LowLevelTransaction.RootObjects,
                    tx.LowLevelTransaction);

                _nextEdgeEtag = ReadLastEtagFrom(tx, EdgeEtagsSlice);
                _nextVertexEtag = ReadLastEtagFrom(tx, VertexEtagsSlice);

                VertexSchema.Create(tx, VerticesSlice, null);
                EdgeSchema.Create(tx, EdgesSlice, null);

                tx.Commit();
            }
        }

        private void OnVertexDataMoved(long previousId, long newId, byte* data, int size)
        {
            //update IDs in adjacency lists
            using (var tx = WriteTransaction())
            {
                var edges = GetEdgesTable(tx);
                foreach (var valueToUpdate in edges.SeekBackwardFrom(EdgeSchema.FixedSizeIndexes[FromSlice], previousId))
                {
                    using (edges.Allocate(out var tvb))
                    {
                        var ptr = valueToUpdate.Reader.Read((int)EdgeTable.Id,out var ptrSize);
                        tvb.Add(ptr,ptrSize);

                        tvb.Add(Bits.SwapBytes(newId)); //update "from"

                        ptr = valueToUpdate.Reader.Read((int)EdgeTable.To,out ptrSize);
                        tvb.Add(ptr,ptrSize);

                        ptr = valueToUpdate.Reader.Read((int)EdgeTable.Etag,out ptrSize);
                        tvb.Add(ptr,ptrSize);

                        ptr = valueToUpdate.Reader.Read((int)EdgeTable.ChangeVector,out ptrSize);
                        tvb.Add(ptr,ptrSize);

                        ptr = valueToUpdate.Reader.Read((int)EdgeTable.Data,out ptrSize);
                        tvb.Add(ptr,ptrSize);

                        edges.Set(tvb);
                    }
                }

                foreach (var valueToUpdate in edges.SeekBackwardFrom(EdgeSchema.FixedSizeIndexes[ToSlice], previousId))
                {
                    using (edges.Allocate(out var tvb))
                    {
                        var ptr = valueToUpdate.Reader.Read((int)EdgeTable.Id,out var ptrSize);
                        tvb.Add(ptr,ptrSize);

                        ptr = valueToUpdate.Reader.Read((int)EdgeTable.From,out ptrSize);
                        tvb.Add(ptr,ptrSize);

                        tvb.Add(Bits.SwapBytes(newId)); //update "to"

                        ptr = valueToUpdate.Reader.Read((int)EdgeTable.Etag,out ptrSize);
                        tvb.Add(ptr,ptrSize);

                        ptr = valueToUpdate.Reader.Read((int)EdgeTable.ChangeVector,out ptrSize);
                        tvb.Add(ptr,ptrSize);

                        ptr = valueToUpdate.Reader.Read((int)EdgeTable.Data,out ptrSize);
                        tvb.Add(ptr,ptrSize);

                        edges.Set(tvb);
                    }
                }

                tx.Commit();
            }
        }

        //TODO: add function to add multiple vertices, kinda like BulkInsert
        public void PutVertex(Transaction tx, long key, string changeVector)
        {
            var table = GetVerticesTable(tx);

            var newEtag = ++_nextVertexEtag;
            using(Slice.From(tx.Allocator, changeVector ?? throw new ArgumentNullException($"{nameof(changeVector)} cannot be null"), out var cv))
            using (table.Allocate(out var tvb))
            {
                tvb.Add(Bits.SwapBytes(key));
                tvb.Add(Bits.SwapBytes(newEtag));
                tvb.Add(cv.Content.Ptr, cv.Size);
                table.Set(tvb);
            }

            WriteNextEtagTo(tx,VertexEtagsSlice, newEtag);
        }

        public bool HasVertex(Transaction tx, long key)
        {
            var table = GetVerticesTable(tx);
            var keyWithSwappedBits = Bits.SwapBytes(key);
            using (Slice.From(tx.Allocator, (byte*) &keyWithSwappedBits, sizeof(long), ByteStringType.Immutable, out var keySlice))
            {
                return table.VerifyKeyExists(keySlice);
            }
        }

        public void DeleteVertex(Transaction tx, long key)
        {
            var table = GetVerticesTable(tx);
            var keyWithSwappedBits = Bits.SwapBytes(key);
            using (Slice.From(tx.Allocator, (byte*) &keyWithSwappedBits, sizeof(long), ByteStringType.Immutable, out var keySlice))
            {
                table.DeleteByPrimaryKey(keySlice, tvh =>
                {
                    var etag = *(long*) tvh.Reader.Read((int) VertexTable.Etag, out var size);
                    Debug.Assert(size == sizeof(long), $"Etags in Vertex table must be {sizeof(long)} bytes, but it is {size}");

                    var hasDeletedEtag = TryDeleteEtagFromWithoutBitSwip(tx, VertexEtagsSlice, etag);
                    Debug.Assert(hasDeletedEtag, $"Didn't find relevant etag = {etag} to delete - this is a possible data corruption");

                    return true;
                });
            }
        }

        private static readonly byte[] dummy_data = new byte[1024 * 1024 * 64];

        public void ConnectBetween(Transaction tx, long fromKey, long toKey, string changeVector)
        {
            var vertexTable = GetVerticesTable(tx);
            var fromKeyWithSwappedBits = Bits.SwapBytes(fromKey);
            var toKeyWithSwappedBits = Bits.SwapBytes(toKey);

            using (Slice.From(tx.Allocator, (byte*) &fromKeyWithSwappedBits, sizeof(long), ByteStringType.Immutable, out var fromKeySlice))
            using (Slice.From(tx.Allocator, (byte*) &toKeyWithSwappedBits, sizeof(long), ByteStringType.Immutable, out var toKeySlice))
            {
                vertexTable.ReadByKey(fromKeySlice, out var fromTvr);
                vertexTable.ReadByKey(toKeySlice, out var toTvr);

                var edgesTable = GetEdgesTable(tx);
                var newEtag = ++_nextEdgeEtag;
                using(Slice.From(tx.Allocator, changeVector ?? throw new ArgumentNullException($"{nameof(changeVector)} cannot be null"), out var cv))
                using (edgesTable.Allocate(out var tvb))
                {
                    var fromBytes = BitConverter.GetBytes(Bits.SwapBytes(fromKey));
                    var toBytes = BitConverter.GetBytes(Bits.SwapBytes(toKey));
                    var keyBytes = fromBytes.Concat(toBytes).ToArray();

                    fixed (byte* idPtr = keyBytes)
                    fixed (byte* dataPtr = dummy_data)
                    {
                        tvb.Add(idPtr, keyBytes.Length);
                        tvb.Add(fromTvr.Id);
                        tvb.Add(toTvr.Id);
                        tvb.Add(newEtag);
                        tvb.Add(cv.Content.Ptr, cv.Size); //change vector
                        tvb.Add(dataPtr, dummy_data.Length); //placeholder for data that is attached to an edge

                        edgesTable.Set(tvb);
                    }
                }

                WriteNextEtagTo(tx,EdgeEtagsSlice, newEtag);
            }

        }


        public Transaction ReadTransaction() => _env.ReadTransaction();
        public Transaction WriteTransaction() => _env.WriteTransaction();

        private static bool TryDeleteEtagFromWithoutBitSwip(Transaction tx, Slice name, long etag)
        {
            using (var fst = new FixedSizeTree(tx.LowLevelTransaction,
                tx.LowLevelTransaction.RootObjects,
                name, 0,
                clone: false))
            {
                var deleteResult = fst.Delete(etag);
                return deleteResult.NumberOfEntriesDeleted > 0;
            }

        }

        private static void WriteNextEtagTo(Transaction tx, Slice name, long etag)
        {
            using (var fst = new FixedSizeTree(tx.LowLevelTransaction,
                tx.LowLevelTransaction.RootObjects,
                name, 0,
                clone: false))
            {
                fst.Add(Bits.SwapBytes(etag));
            }
        }

        private static long ReadLastEtagFrom(Transaction tx, Slice name)
        {
            using (var fst = new FixedSizeTree(tx.LowLevelTransaction,
                tx.LowLevelTransaction.RootObjects,
                name, 0,
                clone: false))
            {
                using (var it = fst.Iterate())
                {
                    if (it.SeekToLast())
                        return Bits.SwapBytes(it.CurrentKey);
                }
            }

            return 0;
        }

        public void Dispose()
        {
            _env.Dispose();
        }
    }
}
