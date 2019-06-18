using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;

namespace Grok
{
    public class GrogEngine
    {
        private static readonly IDictionary<string, string> Types = new Dictionary<string, string>
        {
            {"WORD", @"\w+"},
            {"NUMBER", @"\d+"},
        };

        private static readonly Regex MatchValueRegex = new Regex(@"%{(\w+)((:(\w+)){0,1}|(:(\w+):(int|integer|long|double|boolean)){0,1})}");

        private readonly IDictionary<string, string> _parameterConvert;

        private SemaphoreSlim _semaphore;

        public GrogEngine()
        {
            _parameterConvert = new Dictionary<string, string>();
            _semaphore = new SemaphoreSlim(1, 1);
        }

        private string ReplaceMatch(Match match)
        {
            var replacementPattern = string.Empty;

            var templateName = match.Groups[1].Value;

            if (!Types.ContainsKey(templateName))
                throw new GrokException($"Template {templateName} is not defined.");

            switch (match.Groups[0].Value.Split(':').Length - 1)
            {
                case 0:
                    replacementPattern = $"({Types[templateName]})";
                    break;

                case 1:
                    replacementPattern = $"(?<{match.Groups[4].Value}>{Types[templateName]})";
                    break;

                case 2:
                    replacementPattern = $"(?<{match.Groups[6].Value}>{Types[templateName]})";
                    _parameterConvert.Add(match.Groups[6].Value, match.Groups[7].Value);
                    break;
            }

            return replacementPattern;
        }

        public Dictionary<string, object> ExtractData(string pattern, string text)
        {
            var matchValueEvaluator = new MatchEvaluator(ReplaceMatch);

            _semaphore.Wait();

            _parameterConvert.Clear();

            var replacedPattern = MatchValueRegex.Replace(pattern, matchValueEvaluator);

            var regex = new Regex(replacedPattern);

            var matchCollection = regex.Match(text);

            var result = new Dictionary<string, object>();

            if(!matchCollection.Success) return result;

            var groups = matchCollection.Groups;

            foreach (var groupName in regex.GetGroupNames())
            {
                result.Add(groupName,
                    !_parameterConvert.ContainsKey(groupName)
                        ? groups[groupName].Value
                        : ConvertExtractedData(groupName, groups[groupName].Value));
            }

            _semaphore.Release();

            return result;
        }

        private object ConvertExtractedData(string groupName, string value)
        {
            var convert = _parameterConvert[groupName];

            switch (convert)
            {
                case "int":
                case "integer":
                    return Convert.ToInt32(value);

                case "long":
                    return Convert.ToInt64(value);

                case "double":
                    return Convert.ToDouble(value);

                case "boolean":
                    return Convert.ToBoolean(value);
            }

            return null;
        }
    }
}
