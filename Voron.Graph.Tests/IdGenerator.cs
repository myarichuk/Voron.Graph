using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using System.IO;
using System.Collections.Concurrent;

namespace Voron.Graph.Tests
{
    [TestClass]
    public class IdGeneratorTests  : BaseGraphTest
    {
        protected override void BeforeTestInitialize()
        {
            StorageOptions = StorageEnvironmentOptions.ForPath("\\Data");
        }

        [TestMethod]
        public void Multiple_Hi_value_increases_should_not_break_Id_generation()
        {
            var graph = new GraphEnvironment("TestGraph", Env);

            var nodes = new List<Node>();

            for(int i = 0; i < Constants.HiLoRangeCapacity * 15; i++)
            {
                using(var session = graph.OpenSession())
                {
                    nodes.Add(session.CreateNode(Stream.Null));
                    session.SaveChanges();
                }
            }

            var nodeKeys = nodes.Select(n => n.Key);
            nodeKeys.Should().OnlyHaveUniqueItems();
        }

        [TestMethod]
        public void Multiple_Hi_value_increases_in_parallel_should_not_break_Id_generation()
        {
            var graph = new GraphEnvironment("TestGraph", Env);
            var nodes = new ConcurrentBag<Node>();

            Parallel.For(0, Constants.HiLoRangeCapacity * 75, i =>
            {
                using (var session = graph.OpenSession())
                {
                    nodes.Add(session.CreateNode(Stream.Null));
                    session.SaveChanges();
                }
            });

            var nodeKeys = nodes.Select(n => n.Key);
            nodeKeys.Should().OnlyHaveUniqueItems();
        }
    }
}
