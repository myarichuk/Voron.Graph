using System;
using System.Collections.Generic;

namespace Voron.Graph.Algorithms.MaximumFlow
{
	public class Flow
	{
		private readonly Dictionary<Tuple<long, long>, long> flow;

		public Flow()
		{
			flow = new Dictionary<Tuple<long, long>, long>();
		}

		public long this[Tuple<long, long> key]
		{
			get
			{
				return GetFlowBetween(key.Item1, key.Item2);
			}
			set
			{
				SetFlowBetween(key.Item1, key.Item2, value);
			}
		}

		public long GetFlowBetween(long u, long v)
		{
			var nodeKey = Tuple.Create(u, v);
			long value;
			if (flow.TryGetValue(nodeKey, out value))
			{
				return value;
			}
			else if (flow.TryGetValue(Tuple.Create(v, u), out value))
			{
				return -value;
			}
			else
			{
				flow[nodeKey] = 0;
				return 0;
			}
		}

		public bool Contains(long u, long v)
		{
			return Contains(Tuple.Create(u, v));
		}

		public bool Contains(Tuple<long, long> tuple)
		{
			return flow.ContainsKey(tuple);
		}

		public void SetFlowBetween(long u, long v, long value)
		{
			var nodeKey = Tuple.Create(u, v);
			SetFlowBetween(nodeKey, value);

		}

		public void SetFlowBetween(Tuple<long, long> nodeKey, long value)
		{
			flow[nodeKey] = value;
		}

		public void UpdateFlowBetween(long u, long v, Func<long,long> update)
		{
			var nodeKey = Tuple.Create(u, v);
			var existingValue = flow[nodeKey];
			flow[nodeKey] = update(existingValue);
		}

	}
}
