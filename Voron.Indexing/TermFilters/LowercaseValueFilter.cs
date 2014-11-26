namespace Voron.Indexing.TermFilters
{
	public class LowercaseValueFilter : ITermValueFilter
	{
		public int Order { get; private set; }

		public LowercaseValueFilter(int order)
		{
			Order = order;
		}

		public string ProcessTerm(string term)
		{
			return term.ToLowerInvariant();
		}
	}
}
