using AgentConnectClient.Settings;

namespace AgentConnectClient.Core.Tests;

internal static class SampleSettings
{
    public static ClientSettings Create()
    {
        return new ClientSettings
        {
            McpServerUrl = "https://mcp.example.test/mcp",
            RestBaseUrl = "https://api.example.test",
            AdminKey = "admin-key",
            SavedTokens =
            [
                new SavedToken
                {
                    Token = "token-a",
                    Description = "Ann",
                },
                new SavedToken
                {
                    Token = "token-b",
                    Description = "Ben",
                },
            ],
        };
    }
}
