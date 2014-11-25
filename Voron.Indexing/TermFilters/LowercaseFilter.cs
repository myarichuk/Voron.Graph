namespace Voron.Indexing.TermFilters
{
	public class LowercaseFilter : ITermFilter
	{
		public int Order { get; private set; }

		public LowercaseFilter(int order)
		{
			Order = order;
		}

		public string ProcessTerm(string term)
		{
			return term.ToLowerInvariant();
		}
	}
}
