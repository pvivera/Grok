using System;
using Xunit;
using FluentAssertions;

namespace Grok.Tests
{
    public class GrogEngineTests
    {
        private readonly GrogEngine _sut;

        public GrogEngineTests()
        {
            _sut = new GrogEngine();
        }

        [Fact]
        public void ExtractData_ShouldExtractTheCorrectData()
        {
            var data = _sut.ExtractData(new[] { "%{WORD:method} %{NUMBER:bytes} %{NUMBER:duration}" }, "hello 123 456");

            data.Should().ContainKey("method");
            data["method"].Should().Be("hello");
            data.Should().ContainKey("bytes");
            data["bytes"].Should().Be("123");
            data.Should().ContainKey("duration");
            data["duration"].Should().Be("456");
        }

        [Fact]
        public void ExtractData_ShouldIgnoreCapture_WhenThereIsNoParameterName()
        {
            var data = _sut.ExtractData(new[] { "%{WORD} %{NUMBER:bytes} %{NUMBER:duration}" }, "hello 123 456");

            data.Should().NotContainKey("method");
            data.Should().ContainKey("bytes");
            data["bytes"].Should().Be("123");
            data.Should().ContainKey("duration");
            data["duration"].Should().Be("456");
        }

        [Fact]
        public void ExtractData_ShouldExtractTheCorrectDataAndConvert()
        {
            var data = _sut.ExtractData(new[] { "%{WORD:method} %{NUMBER:bytes:int} %{NUMBER:duration:integer}" }, "hello 123 456");

            data.Should().ContainKey("method");
            data["method"].Should().Be("hello");
            data.Should().ContainKey("bytes");
            data["bytes"].Should().Be(123);
            data.Should().ContainKey("duration");
            data["duration"].Should().Be(456);
        }

        [Fact]
        public void ExtractData_ShouldThrowGrokException_WhenTemplateIsNotDefined()
        {
            Action action = () => _sut.ExtractData(new[] { "%{TEST}" }, "text");

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
            var result = _sut.ExtractData(patterns, text);

            result["number"].Should().Be(expected);
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
            Action action = () => _sut.ExtractData(patterns, text);

            action.Should().Throw<GrokException>().WithMessage("*The parameter number cannot be convert to*");
        }

    }
}
