using AgentConnectClient.Calling;

namespace AgentConnectClient.Core.Tests;

public sealed class CleartextHttpTests
{
    [Theory]
    [InlineData("http://example.test")]
    [InlineData("HTTP://example.test/mcp")]
    [InlineData(" http://127.0.0.1:9/ ")]
    public void IsHttp_HttpUrl_IsTrue(string url)
    {
        Assert.True(CleartextHttp.IsHttp(url));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://example.test")]
    [InlineData("example.test")]
    public void IsHttp_HttpsEmptyOrRelative_IsFalse(string url)
    {
        Assert.False(CleartextHttp.IsHttp(url));
    }
}
