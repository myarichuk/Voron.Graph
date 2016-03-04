using System;
using System.IO;
using Xunit;

namespace Voron.Graph.Tests
{
    public class BasicTests
    {
        [Fact]
        public void Initialization_should_work()
        {
            //create in-memory 
            using (var env = new StorageEnvironment())
            { }

            //create persisted
            var tempPath = Path.GetTempPath() + Path.DirectorySeparatorChar + Guid.NewGuid();
            try
            {
                using (var env = new StorageEnvironment(tempPath))
                { }
            }
            finally
            {
                Directory.Delete(tempPath,true);
            }
        }
    }
}
