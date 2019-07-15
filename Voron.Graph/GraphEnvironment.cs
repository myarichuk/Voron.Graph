using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Voron.Data.Fixed;
using Voron.Data.RawData;
using Voron.Impl;

namespace Voron.Graph
{
    public class GraphEnvironment : IDisposable
    {
        private const string DataTreeName = "Data";
        private const string VertexSectionPages = "VertexSectionPages";
        
        private readonly StorageEnvironment _env;
        

        #region Extract embedded librvn assemblies
        static GraphEnvironment()
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
                        var dllPath = Path.Combine(dirName, embeddedDll.Replace("Voron.Graph.librvnpal.",string.Empty));
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
        {
            _env = new StorageEnvironment(environmentOptions);
            Initialize();
        }

        private void Initialize()
        {
            using (var tx = _env.WriteTransaction())
            {
                var dataTree = tx.CreateTree(DataTreeName);

                using (Slice.From(tx.Allocator, VertexSectionPages, out var verticesSectionsSlice))
                {
                    var verticesSectionsTree = new FixedSizeTree(tx.LowLevelTransaction, dataTree,
                                                        verticesSectionsSlice, 0);

                    if (verticesSectionsTree.NumberOfEntries == 0)
                    {
                        var section = ActiveRawDataSmallSection.Create(tx.LowLevelTransaction, VertexSectionPages, 0);
                        verticesSectionsTree.Add(section.PageNumber);
                    }
                }

                tx.Commit();
            }
        }

        public Transaction ReadTransaction() => _env.ReadTransaction();
        public Transaction WriteTransaction() => _env.WriteTransaction();



        public void Dispose()
        {
            _env.Dispose();
        }
    }
}
