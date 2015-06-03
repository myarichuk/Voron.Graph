using System;

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
