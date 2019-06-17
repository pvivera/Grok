using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Grok
{
    public class GrogEngine
    {
        private static IDictionary<string, string> Types = new Dictionary<string, string>
        {
            {"WORD", @"\w+"},
            {"NUMBER", @"\d+"},
        };

        private static Regex MatchValueRegex = new Regex(@"%{(\w+):(\w+)}");
        private static MatchEvaluator MatchValueEvaluator = match => string.Format("(?<{0}>{1})", match.Groups[2].Value, Types[match.Groups[1].Value]);

        public static Dictionary<string, string> ExtractData(string pattern, string text)
        {
            var regex = new Regex(MatchValueRegex.Replace(pattern, MatchValueEvaluator));

            var matchCollection = regex.Match(text);

            var result = new Dictionary<string, string>();

            if(!matchCollection.Success) return result;

            var groups = matchCollection.Groups;

            foreach (string groupName in regex.GetGroupNames())
            {
                result.Add(groupName, groups[groupName].Value);
            }

            return result;
        }
    }
}
