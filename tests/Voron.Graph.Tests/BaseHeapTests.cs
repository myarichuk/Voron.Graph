using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Voron.Graph.Collections;
using Xunit;

namespace Voron.Graph.Tests
{
	public abstract class BaseHeapTests
	{
		public abstract Func<IPriorityQueue<int,int>> CreateHeap { get; }

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
		public void GetMinAndDelete_should_fetch_sorted(int[] data)
		{
			var kvpData = data.Select(x => new HeapItem<int, int>(x, x)).ToArray();
			var heap = CreateHeap?.Invoke();
			Assert.NotNull(heap); //precaution

			foreach (var x in kvpData)
				heap.Insert(x.Priority,x.Value);

			Array.Sort(data);

			var fetchedData = new List<int>();

			while (!heap.IsEmpty)
				fetchedData.Add(heap.GetMinAndDelete());

			fetchedData.Should().ContainInOrder(data);
		}

		[Fact]
		public void Reduce_priority_should_lower_the_item_to_new_priority_while_keeping_sorting_correct1()
		{
			var data = new[]
			{
				new HeapItem<int, int>(3,0),
				new HeapItem<int, int>(6,1),
				new HeapItem<int, int>(12,4),
				new HeapItem<int, int>(24,7)
			};

			var heap = CreateHeap?.Invoke();
			Assert.NotNull(heap); //precaution

			foreach (var x in data)
				heap.Insert(x.Priority, x.Value);

			data[1].Priority = 14;
			var tmp = data[2];
			data[2] = data[1];
			data[1] = tmp;

			var fetchedData = new List<int>();

			heap.ChangePriority(6, 14);

			while (!heap.IsEmpty)
				fetchedData.Add(heap.GetMinAndDelete());

			var assertData = data.Select(x => x.Value).ToArray();
			fetchedData.Should().ContainInOrder(assertData);
		}

		[Fact]
		public void Reduce_priority_should_lower_the_item_to_new_priority_while_keeping_sorting_correct2()
		{
			var data = new List<HeapItem<int, int>>
			{
				new HeapItem<int, int>(3,0),
				new HeapItem<int, int>(6,1),
				new HeapItem<int, int>(12,2),
				new HeapItem<int, int>(14,3),
				new HeapItem<int, int>(16,4),
				new HeapItem<int, int>(18,5),
				new HeapItem<int, int>(24,6)
			};

			var heap = CreateHeap?.Invoke();
			Assert.NotNull(heap); //precaution

			foreach (var x in data)
				heap.Insert(x.Priority, x.Value);

			data[4].Priority = 9;
			var tmp = data[4];

			data.RemoveAt(4);
			data.Insert(2, tmp);

			var fetchedData = new List<int>();

			heap.ChangePriority(16, 9);

			while (!heap.IsEmpty)
				fetchedData.Add(heap.GetMinAndDelete());

			var assertData = data.Select(x => x.Value).ToArray();
			fetchedData.Should().ContainInOrder(assertData);
		}
	}
}
