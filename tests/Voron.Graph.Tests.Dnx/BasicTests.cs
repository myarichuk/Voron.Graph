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

        [Fact]
        public void Simple_vertex_read_write_should_work()
        {
            using (var storage = new GraphStorage())
            {
                long id1,id2;
                using (var tx = storage.WriteTransaction())
				using (var data1 = new MemoryStream(new byte[] { 1, 2, 3 }))
				using (var data2 = new MemoryStream(new byte[] { 3, 2, 1 }))
				{
					id1 = storage.AddVertex(tx, data1);
					id2 = storage.AddVertex(tx, data2);
					tx.Commit();
                }

				using (var tx = storage.ReadTransaction())
				{
					using (var data1 = storage.ReadVertex(tx, id1))					
						Assert.Equal(new byte[] { 1, 2, 3 }, data1.ReadToEnd());
					using (var data2 = storage.ReadVertex(tx, id2))
						Assert.Equal(new byte[] { 3, 2, 1 }, data2.ReadToEnd());
				}
			}
        }
    }
}
