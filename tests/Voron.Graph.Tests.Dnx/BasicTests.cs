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
				{
					id1 = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
					id2 = storage.AddVertex(tx, new byte[] { 3, 2, 1 });
					tx.Commit();
                }

				using (var tx = storage.ReadTransaction())
				{
					var data1 = storage.ReadVertex(tx, id1);
					Assert.NotNull(data1);
					Assert.Equal(new byte[] { 1, 2, 3 }, data1);
					var data2 = storage.ReadVertex(tx, id2);
					Assert.NotNull(data2);
					Assert.Equal(new byte[] { 3, 2, 1 }, data2);
				}
			}
        }

		[Fact]
		public void Simple_vertex_delete_should_work()
		{
			using (var storage = new GraphStorage())
			{
				long id;
				using (var tx = storage.WriteTransaction())
				{
					id = storage.AddVertex(tx, new byte[] { 1, 2, 3 });
					tx.Commit();
				}

				using (var tx = storage.WriteTransaction())
				{
					storage.DeleteVertex(tx, id);
					tx.Commit();
				}

				using (var tx = storage.ReadTransaction())
					Assert.Null(storage.ReadVertex(tx, id));
			}
		}
	}
}
