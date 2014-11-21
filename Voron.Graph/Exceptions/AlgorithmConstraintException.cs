using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voron.Graph.Exceptions
{
    public class AlgorithmConstraintException : Exception
    {
        public AlgorithmConstraintException(string message)
            :base(message)
        {			
        }
    }
}
