namespace Voron.Indexing.TermFilters
{
	public class WhitespaceValueFilter : ITermValueFilter
	{
		public WhitespaceValueFilter(int order)
		{
			Order = order;
		}

		public int Order { get; private set; }

		public string ProcessTerm(string term)
		{
			return term.Replace(" ", "");
		}
	}
}
