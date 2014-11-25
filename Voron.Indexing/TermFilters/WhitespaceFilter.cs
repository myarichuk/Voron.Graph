namespace Voron.Indexing.TermFilters
{
	public class WhitespaceFilter : ITermFilter
	{
		public WhitespaceFilter(int order)
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
