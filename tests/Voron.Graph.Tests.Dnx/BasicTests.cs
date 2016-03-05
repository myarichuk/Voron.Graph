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
            using (var storage = new GraphStorage())
            { }

            //create persisted
            var tempPath = Path.GetTempPath() + Path.DirectorySeparatorChar + Guid.NewGuid();
            try
            {
                using (var storage = new GraphStorage(tempPath))
                {
                }
            }
            finally
            {
                Directory.Delete(tempPath,true);
            }
        }
    }
}
