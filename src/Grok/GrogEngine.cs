using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Grok
{
    public class GrogEngine
    {
        private static IDictionary<string, string> Templates = new Dictionary<string, string>
        {
            {"WORD", @"\w+"},
            {"NUMBER", @"(?:%{BASE10NUM})"},
            {"BASE10NUM", "(?<![0-9.+-])(?>[+-]?(?:(?:[0-9]+(?:\\.[0-9]+)?)|(?:\\.[0-9]+)))" }
        };

        private static readonly Regex MatchValueRegex = new Regex(@"%{(\w+)((:(\w+)){0,1}|(:(\w+):(int|integer|long|decimal|double|boolean)){0,1})}");

        private readonly IDictionary<string, string> _parameterConvert;

        private readonly SemaphoreSlim _semaphore;

        public GrogEngine()
        {
            _parameterConvert = new Dictionary<string, string>();
            _semaphore = new SemaphoreSlim(1, 1);

            ProcessTemplates();
        }

        private void ProcessTemplates()
        {
            var processPattern = new Regex(@"%{(\w+)}", RegexOptions.Compiled);
            var templatesToProcess = Templates.Where(t => processPattern.IsMatch(t.Value))
                .Select(t => new {TemplateName = t.Key, Match = processPattern.Match(t.Value)})
                .ToList();

            foreach (var templateToProcess in templatesToProcess)
            {
                for (var i = 1; i < templateToProcess.Match.Groups.Count; i++)
                {
                    var templateName = templateToProcess.Match.Groups[i].Value;
                    Templates[templateToProcess.TemplateName] =
                        Templates[templateToProcess.TemplateName].Replace($"%{{{templateName}}}", Templates[templateName]);
                }
            }
        }

        private string ReplaceMatch(Match match)
        {
            var replacementPattern = string.Empty;

            var templateName = match.Groups[1].Value;

            if (!Templates.ContainsKey(templateName))
                throw new GrokException($"Template {templateName} is not defined.");

            switch (match.Groups[0].Value.Split(':').Length - 1)
            {
                case 0:
                    replacementPattern = $"({Templates[templateName]})";
                    break;

                case 1:
                    replacementPattern = $"(?<{match.Groups[4].Value}>{Templates[templateName]})";
                    break;

                case 2:
                    replacementPattern = $"(?<{match.Groups[6].Value}>{Templates[templateName]})";
                    _parameterConvert.Add(match.Groups[6].Value, match.Groups[7].Value);
                    break;
            }

            return replacementPattern;
        }

        public Dictionary<string, object> ExtractData(string[] patterns, string text)
        {
            var matchValueEvaluator = new MatchEvaluator(ReplaceMatch);

            _semaphore.Wait();

            _parameterConvert.Clear();

            var results = new Dictionary<string, object>();

            foreach(var pattern in patterns)
            {
                var result = ProcessPattern(matchValueEvaluator, pattern, text);

                if (!result.Any()) continue;

                foreach(var k in result.Keys)
                {
                    if (results.Keys.Contains(k))
                    {
                        results.Add(k, result[k]);
                    }
                    else
                    {
                        results[k] = result[k];
                    }
                }
            }

            _semaphore.Release();

            return results;
        }

        private Dictionary<string, object> ProcessPattern(MatchEvaluator matchValueEvaluator, string pattern, string text)
        {
            var replacedPattern = MatchValueRegex.Replace(pattern, matchValueEvaluator);

            var regex = new Regex(replacedPattern);

            var matchCollection = regex.Match(text);

            var result = new Dictionary<string, object>();

            if (!matchCollection.Success) return result;

            var groups = matchCollection.Groups;

            foreach (var groupName in regex.GetGroupNames())
            {
                result.Add(groupName,
                    !_parameterConvert.ContainsKey(groupName)
                        ? groups[groupName].Value
                        : ConvertExtractedData(groupName, groups[groupName].Value));
            }

            return result;
        }

        private object ConvertExtractedData(string groupName, string value)
        {
            var convert = _parameterConvert[groupName];

            switch (convert)
            {
                case "int":
                case "integer":
                    if (Int32.TryParse(value, out var resultInt))
                        return resultInt;
                    ThrowConversionException(groupName, convert, value);
                    break;

                case "long":
                    if (Int64.TryParse(value, out var resultInt64))
                        return resultInt64;
                    ThrowConversionException(groupName, convert, value);
                    break;

                case "decimal":
                    if (Decimal.TryParse(value, out var resultDecimal))
                        return resultDecimal;
                    ThrowConversionException(groupName, convert, value);
                    break;

                case "double":
                    if (Double.TryParse(value, out var resultDouble))
                        return resultDouble;
                    ThrowConversionException(groupName, convert, value);
                    break;

                case "boolean":
                    if (Boolean.TryParse(value, out var resultBoolean))
                        return resultBoolean;
                    ThrowConversionException(groupName, convert, value);
                    break;
            }

            return null;
        }

        private void ThrowConversionException(string parameter, string convert, string value)
        {
            throw new GrokException($"The parameter {parameter} cannot be convert to {convert} ({value})");
        }
    }
}
