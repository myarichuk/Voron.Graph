using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class BaseGraphTest
    {
        protected StorageEnvironment Env;
        protected ConcurrentQueue<IDisposable> DisposalQueue;

        protected StorageEnvironmentOptions StorageOptions;

        protected virtual void BeforeTestInitialize() { }

        [TestInitialize]
        public void BeforeTest()
        {
            Env = new StorageEnvironment(StorageOptions ?? StorageEnvironmentOptions.CreateMemoryOnly());
            DisposalQueue = new ConcurrentQueue<IDisposable>();
        }

        [TestCleanup]
        public void AfterTest()
        {
            Env.Dispose();
            foreach (var disposable in DisposalQueue.Where(x => x != null))
                disposable.Dispose();
        }

        public Stream StreamFrom(string s)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(s));
            //just to be sure
            DisposalQueue.Enqueue(stream);

            return stream;
        }

        public string StringFrom(Stream stream)
        {
            var buffer = new byte[128];
            var readBytes = new List<byte>();
            while (stream.Read(buffer, 0, 128) > 0)
                readBytes.AddRange(buffer.Where(x => x != 0));

            DisposalQueue.Enqueue(stream);
            return Encoding.UTF8.GetString(readBytes.ToArray());
        }
    }
}
