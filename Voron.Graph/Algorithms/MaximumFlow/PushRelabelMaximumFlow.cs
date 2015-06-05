using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voron.Graph.Algorithms.MaximumFlow
{
	public class PushRelabelMaximumFlow : BaseMaximumFlow
	{
		//basically a function [node id] -> label
		private readonly Dictionary<long, long> labeling;

		private readonly Flow preflow;

		public PushRelabelMaximumFlow(Func<Edge, long> capacity)
			: base(capacity)
		{
			labeling = new Dictionary<long, long>();
			preflow = new Flow();
        }

		public override long MaximumFlow()
		{
			throw new NotImplementedException();
		}

		public override Task<long> MaximumFlowAsync()
		{
			throw new NotImplementedException();
		}
	}
}
