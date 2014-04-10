using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class BaseGraphTest
    {
        protected StorageEnvironment Env;

        [TestInitialize]
        public void BeforeTest()
        {
            Env = new StorageEnvironment(StorageEnvironmentOptions.CreateMemoryOnly());
        }

        [TestCleanup]
        public void AfterTest()
        {
            Env.Dispose();
        }

        public Stream StreamFrom(string s)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(s));
        }

        public string StringFrom(Stream stream)
        {
            var buffer = new byte[128];
            var readBytes = new List<byte>();
            while (stream.Read(buffer, 0, 128) > 0)
                readBytes.AddRange(buffer.Where(x => x != 0));

            return Encoding.UTF8.GetString(readBytes.ToArray());
        }
    }
}
