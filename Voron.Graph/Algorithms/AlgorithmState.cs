using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Algorithms
{
    public enum AlgorithmState : int
    {
        NotStarted = 0,
        Running = 1,
        Finished = 2,
        Aborted = 3
    }
}
