using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using System.Threading;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class EtagTests
    {
        [TestMethod]
        public void Concurrent_etag_creations_should_result_in_unique_etags()
        {
            var etags = new ConcurrentBag<Etag>();

            Parallel.For(0, 1000, i =>
            {
                for (int j = 0; j < 1000; j++)
                    etags.Add(Etag.Generate());
            });

            etags.Should().OnlyHaveUniqueItems("Because concurrently created Etags must be unique");            
        }

        [TestMethod]
        public void Older_etag_should_be_smaller_in_comarison_to_recently_created1()
        {
            var oldEtag = Etag.Generate();
            Thread.Sleep(20); //DateTime.UtcNow has 15ms resolution
            var recentEtag1 = Etag.Generate();
            Thread.Sleep(20); //DateTime.UtcNow has 15ms resolution
            var recentEtag2 = Etag.Generate();

            Assert.IsTrue(recentEtag1 > oldEtag);
            Assert.IsTrue(recentEtag2 > recentEtag1);
            Assert.IsTrue(recentEtag1 != oldEtag);
        }

        [TestMethod]
        public void Older_etag_should_be_smaller_in_comarison_to_recently_created2()
        {
            var oldEtag = Etag.Generate();
            var recentEtag1 = Etag.Generate();
            var recentEtag2 = Etag.Generate();

            Assert.IsTrue(recentEtag1 > oldEtag);
            Assert.IsTrue(recentEtag2 > recentEtag1);
            Assert.IsTrue(recentEtag1 != oldEtag);
        }
    }
}
