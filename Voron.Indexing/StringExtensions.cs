using System.Collections.Concurrent;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace Voron.Indexing
{
	public static class StringExtensions
	{
		private static readonly ConcurrentDictionary<string, Regex> RegexCache = new ConcurrentDictionary<string, Regex>();
		public static bool IsRegexMatch(this string str, string regexPattern, bool ignoreCase = true)
		{
			var options = RegexOptions.Compiled;
			if (ignoreCase)
				options |= RegexOptions.IgnoreCase;
			var regex = RegexCache.GetOrAdd(regexPattern, pattern => new Regex(pattern, options));
			return regex.IsMatch(str);
		}
	}
}
