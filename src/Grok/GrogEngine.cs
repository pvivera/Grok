using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Grok
{
    public class GrogEngine
    {
        private static readonly IDictionary<string, string> Types = new Dictionary<string, string>
        {
            {"WORD", @"\w+"},
            {"NUMBER", @"\d+"},
        };

        private static readonly Regex MatchValueRegex = new Regex(@"%{(\w+)(:(\w+)){0,1}}");

        private static readonly MatchEvaluator MatchValueEvaluator = match => match.Groups[2].Success
            ? $"(?<{match.Groups[3].Value}>{Types[match.Groups[1].Value]})"
            : $"({Types[match.Groups[1].Value]})";

        public static Dictionary<string, string> ExtractData(string pattern, string text)
        {
            var replacedPattern = MatchValueRegex.Replace(pattern, MatchValueEvaluator);

            var regex = new Regex(replacedPattern);

            var matchCollection = regex.Match(text);

            var result = new Dictionary<string, string>();

            if(!matchCollection.Success) return result;

            var groups = matchCollection.Groups;

            foreach (var groupName in regex.GetGroupNames())
            {
                result.Add(groupName, groups[groupName].Value);
            }

            return result;
        }
    }
}
