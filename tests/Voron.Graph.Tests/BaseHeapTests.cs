using FluentAssertions;
using System;
using System.Collections.Generic;
using Voron.Graph.Collections;
using Xunit;

namespace Voron.Graph.Tests
{
	public abstract class BaseHeapTests
	{
		public abstract Func<IPriorityQueue<int>> CreateHeap { get; }

		[Theory]
		[InlineData(new int[0])] //kind of obvious, but still...
		[InlineData(new[] { 1 })]
		[InlineData(new[] { 1, 2 })]
		[InlineData(new[] { 3, 2, 1 })]
		[InlineData(new[] { 5, 4, 3, 2, 1, 0 })]
		[InlineData(new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 })]
		[InlineData(new[] { 7, 5, 3, 8, 0, 1, 10, 6, 2, 4, 9 })]
		[InlineData(new[] { 1, 3, 22, 15, 2, 3, 5, 4, 2, 1, 12, 4, 88, 11, 7, 5 })]
		[InlineData(new[] { 33, 11, 22, 33, 44, 22, 11, 33 })]
		public void Adding_random_values_to_priority_queue_and_getting_min_should_return_sorted_data(int[] data)
		{
			var heap = CreateHeap?.Invoke();
			Assert.NotNull(heap); //precaution

			foreach (var x in data)
				heap.Insert(x);

			Array.Sort(data);

			var fetchedData = new List<int>();

			while (!heap.IsEmpty)
				fetchedData.Add(heap.GetMinAndDelete());

			for (int i = 0; i < fetchedData.Count; i++)
				fetchedData[i].Should().Be(data[i]);
		}
	}
}
