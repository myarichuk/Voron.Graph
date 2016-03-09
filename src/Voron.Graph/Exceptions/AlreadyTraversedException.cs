using System;

namespace Voron.Graph
{
	internal class AlreadyTraversedException : Exception
	{
		public AlreadyTraversedException()
		{
		}

		public AlreadyTraversedException(string message) : base(message)
		{
		}

		public AlreadyTraversedException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}