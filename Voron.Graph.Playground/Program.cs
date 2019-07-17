using System;
using System.IO;

namespace Voron.Graph.Playground
{
    public static class Program
    {
        static void Main(string[] args)
        {
            using (var graph = new GraphEnvironment(true))
            {
                using (var tx = graph.WriteTransaction())
                {
                    graph.PutVertex(tx, 1, "");
                    graph.PutVertex(tx, 2, "");

                    graph.ConnectBetween(tx, 1, 2, "");

                    tx.Commit();
                }

                for (int i = 2; i < 500_000_000; i++)
                {
                    using (var tx = graph.WriteTransaction())
                    {
                        for (int j = 0; j < 1000; j++)
                        {
                            graph.PutVertex(tx, i+j, "");
                        }
                        tx.Commit();
                    }

                    if(i % 500 == 0)
                        Console.WriteLine(i);
                }

                using (var tx = graph.WriteTransaction())
                {
                    graph.DeleteVertex(tx, 1);
                    graph.DeleteVertex(tx, 2);
                    tx.Commit();
                }
            }
        }
    }
}
