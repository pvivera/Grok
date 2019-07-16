using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace Grok.Tests
{
    public class GrogEngineTests
    {
        private readonly GrogEngine _sut;

        public GrogEngineTests()
        {
            _sut = new GrogEngine();
        }

        private (GrokConfiguration Configuration, JObject InputData) GetTestData(string property, string[] patterns, string text, string propertyInFilter = "")
        {
            var inputData = new JObject
            {
                {property, text}
            };

            var configuration = new GrokConfiguration
            {
                Filters = new[]
                {
                    new GrokFilter
                    {
                        Property = string.IsNullOrWhiteSpace(propertyInFilter) ? property : propertyInFilter,
                        Patterns = patterns
                    }
                }
            };

            return (configuration, inputData);
        }

        [Fact]
        public void ExtractData_ShouldExtractTheCorrectData()
        {
            var testData = GetTestData("test", new[] {"%{WORD:method} %{NUMBER:bytes} %{NUMBER:duration}"}, "hello 123 456");
            
            var result = _sut.ExtractData(testData.Configuration, testData.InputData);

            result.DataExtracted.Should().BeTrue();
            result.Data.Should().ContainKey("method");
            result.Data["method"].Value<string>().Should().Be("hello");
            result.Data.Should().ContainKey("bytes");
            result.Data["bytes"].Value<string>().Should().Be("123");
            result.Data.Should().ContainKey("duration");
            result.Data["duration"].Value<string>().Should().Be("456");
        }

        [Fact]
        public void ExtractData_ShouldIgnoreCapture_WhenThereIsNoParameterName()
        {
            var testData = GetTestData("test", new[] {"%{WORD} %{NUMBER:bytes} %{NUMBER:duration}"}, "hello 123 456");

            var result = _sut.ExtractData(testData.Configuration, testData.InputData);

            result.DataExtracted.Should().BeTrue();
            result.Data.Property("method").Should().BeNull();
            result.Data.Should().ContainKey("bytes");
            result.Data["bytes"].Value<string>().Should().Be("123");
            result.Data.Should().ContainKey("duration");
            result.Data["duration"].Value<string>().Should().Be("456");
        }

        [Fact]
        public void ExtractData_ShouldExtractTheCorrectDataAndConvert()
        {
            var testData = GetTestData("test", new[] {"%{WORD:method} %{NUMBER:bytes:int} %{NUMBER:duration:integer}"},
                "hello 123 456");

            var result = _sut.ExtractData(testData.Configuration, testData.InputData);

            result.DataExtracted.Should().BeTrue();
            result.Data.Should().ContainKey("method");
            result.Data["method"].Value<string>().Should().Be("hello");
            result.Data.Should().ContainKey("bytes");
            result.Data["bytes"].Value<int>().Should().Be(123);
            result.Data.Should().ContainKey("duration");
            result.Data["duration"].Value<int>().Should().Be(456);
        }

        [Fact]
        public void ExtractData_ShouldThrowGrokException_WhenTemplateIsNotDefined()
        {
            var testData = GetTestData("test", new[] {"%{TEST}"}, "hello 123 456");

            Action action = () => _sut.ExtractData(testData.Configuration, testData.InputData);

            action.Should().Throw<GrokException>().WithMessage("*TEST*");
        }

        [Theory]
        [InlineData("123", 123, "%{NUMBER:number:int}")]
        [InlineData("123", 123, "%{NUMBER:number:integer}")]
        [InlineData("123456789", 123456789, "%{NUMBER:number:long}")]
        [InlineData("12.34", 12.34, "%{NUMBER:number:decimal}")]
        [InlineData("12.34", 12.34, "%{NUMBER:number:double}")]
        [InlineData("true", true, "%{WORD:number:boolean}")]
        public void ExtractData_ShouldParseAndConvertData(string text, object expected, params string[] patterns)
        {
            var testData = GetTestData("test", patterns, text);

            var result = _sut.ExtractData(testData.Configuration, testData.InputData);

            result.DataExtracted.Should().BeTrue();
            result.Data["number"].Value<object>().Should().Be(expected);
        }

        [Theory]
        [InlineData("abc", "%{WORD:number:int}")]
        [InlineData("abc", "%{WORD:number:integer}")]
        [InlineData("abc", "%{WORD:number:long}")]
        [InlineData("abc", "%{WORD:number:decimal}")]
        [InlineData("abc", "%{WORD:number:double}")]
        [InlineData("abc", "%{WORD:number:boolean}")]
        public void ExtractData_ShouldThrowGrokException_WhenConvertionIsInvalid(string text, params string[] patterns)
        {
            var testData = GetTestData("test", patterns, text);

            Action action = () => _sut.ExtractData(testData.Configuration, testData.InputData);

            action.Should().Throw<GrokException>().WithMessage("*The parameter number cannot be convert to*");
        }

        [Theory]
        [InlineData("abc def 123", "%{WORD:word} %{WORD:word2}", "%{NUMBER:number:int}")]
        public void ExtractData_ShouldExtract_UsingMultiplePatterns(string text, params string[] patterns)
        {
            var testData = GetTestData("test", patterns, text);

            var result = _sut.ExtractData(testData.Configuration, testData.InputData);

            result.DataExtracted.Should().BeTrue();
            result.Data.Should().ContainKey("word");
            result.Data["word"].Value<string>().Should().Be("abc");
            result.Data.Should().ContainKey("number");
            result.Data["number"].Value<int>().Should().Be(123);
        }

        [Fact]
        public void ExtractData_ShouldThrowGrokException_WhenGrokConfigurationIsNull()
        {
            Action action = () => _sut.ExtractData(default(GrokConfiguration), null);

            action.Should().Throw<GrokException>();
        }

        [Fact]
        public void ExtractData_ShouldReturnAnEmptyGrokResult_WhenInputObjectIsNull()
        {
            var result = _sut.ExtractData(new GrokConfiguration(), null);

            result.Should().NotBeNull();
            result.DataExtracted.Should().BeFalse();
        }

        [Fact]
        public void ExtractData_ShouldReturnAnEmptyGrokResult_WhenNoFieldsWereFoundFromTheConfiguration()
        {
            var testData = GetTestData("test", new[] {"123"}, "123", "message");

            var result = _sut.ExtractData(testData.Configuration, testData.InputData);

            result.Should().NotBeNull();
            result.DataExtracted.Should().BeFalse();
            result.Data.ContainsKey("test").Should().BeTrue();
            result.Data.ContainsKey("message").Should().BeFalse();
        }

        [Theory]
        [InlineData("abc", "%{WORD:word}", "%{WORD:word2}")]
        public void ExtractData_ShouldExtract_UsingMultiplePatternsButBreakOnMatch(string text, params string[] patterns)
        {
            var testData = GetTestData("test", patterns, text);
            testData.Configuration.Filters.FirstOrDefault().BreakOnMatch = true;

            var result = _sut.ExtractData(testData.Configuration, testData.InputData);

            result.DataExtracted.Should().BeTrue();
            result.Data.Should().ContainKey("word");
            result.Data["word"].Value<string>().Should().Be("abc");
            result.Data.Should().NotContainKey("word2");
        }
    }
}
