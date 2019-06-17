using Xunit;
using FluentAssertions;

namespace Grok.Tests
{
    public class GrogEngineTests
    {
        [Fact]
        public void ExtractData_ShouldExtractTheCorrectData()
        {
            var data = GrogEngine.ExtractData("%{WORD:method} %{NUMBER:bytes} %{NUMBER:duration}", "hello 123 456");

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
            var data = GrogEngine.ExtractData("%{WORD} %{NUMBER:bytes} %{NUMBER:duration}", "hello 123 456");

            data.Should().NotContainKey("method");
            data.Should().ContainKey("bytes");
            data["bytes"].Should().Be("123");
            data.Should().ContainKey("duration");
            data["duration"].Should().Be("456");
        }
    }
}
