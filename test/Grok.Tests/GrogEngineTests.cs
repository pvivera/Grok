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
            var data = _sut.ExtractData("%{WORD:method} %{NUMBER:bytes} %{NUMBER:duration}", "hello 123 456");

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
            var data = _sut.ExtractData("%{WORD} %{NUMBER:bytes} %{NUMBER:duration}", "hello 123 456");

            data.Should().NotContainKey("method");
            data.Should().ContainKey("bytes");
            data["bytes"].Should().Be("123");
            data.Should().ContainKey("duration");
            data["duration"].Should().Be("456");
        }

        [Fact]
        public void ExtractData_ShouldExtractTheCorrectDataAndConvert()
        {
            var data = _sut.ExtractData("%{WORD:method} %{NUMBER:bytes:int} %{NUMBER:duration:integer}", "hello 123 456");

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
            Action action = () => _sut.ExtractData("%{TEST}", "text");

            action.Should().Throw<GrokException>().WithMessage("*TEST*");
        }

        [Theory]
        [InlineData("%{NUMBER:number:int}", "123", 123)]
        [InlineData("%{NUMBER:number:integer}", "123", 123)]
        [InlineData("%{NUMBER:number:long}", "123456789", 123456789)]
        [InlineData("%{NUMBER:number:decimal}", "12.34", 12.34)]
        [InlineData("%{NUMBER:number:double}", "12.34", 12.34)]
        [InlineData("%{WORD:number:boolean}", "true", true)]
        public void ExtractData_ShouldParseAndConvertData(string pattern, string text, object expected)
        {
            var result = _sut.ExtractData(pattern, text);

            result["number"].Should().Be(expected);
        }
        [Theory]
        [InlineData("%{WORD:number:int}", "abc")]
        [InlineData("%{WORD:number:integer}", "abc")]
        [InlineData("%{WORD:number:long}", "abc")]
        [InlineData("%{WORD:number:decimal}", "abc")]
        [InlineData("%{WORD:number:double}", "abc")]
        [InlineData("%{WORD:number:boolean}", "abc")]
        public void ExtractData_ShouldThrowGrokException_WhenConvertionIsInvalid(string pattern, string text)
        {
            Action action = () => _sut.ExtractData(pattern, text);

            action.Should().Throw<GrokException>().WithMessage("*The parameter number cannot be convert to*");
        }

    }
}
