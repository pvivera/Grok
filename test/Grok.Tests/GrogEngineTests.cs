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
    }
}
