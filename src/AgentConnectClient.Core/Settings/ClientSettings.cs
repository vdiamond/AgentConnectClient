namespace AgentConnectClient.Settings;

/// <summary>
/// The values stored in the per-user settings file.
/// </summary>
public sealed record ClientSettings
{
    /// <summary>
    /// Gets the full MCP Streamable HTTP URL. Empty until the user sets it.
    /// </summary>
    public string McpServerUrl { get; init; } = "";

    /// <summary>
    /// Gets the REST base URL. Empty until the user sets it.
    /// </summary>
    public string RestBaseUrl { get; init; } = "";

    /// <summary>
    /// Gets the platform admin key sent as <c>X-Admin-Key</c>.
    /// </summary>
    public string AdminKey { get; init; } = "";

    /// <summary>
    /// Gets the saved Member tokens, in the order they were added.
    /// </summary>
    public IReadOnlyList<SavedToken> SavedTokens { get; init; } = [];

    /// <summary>
    /// Gets the token at <paramref name="index"/>, or <see langword="null"/> when none is selected.
    /// </summary>
    /// <param name="index">The index in <see cref="SavedTokens"/>.</param>
    /// <returns>The selected token, or <see langword="null"/>.</returns>
    public SavedToken? GetSelectedToken(int? index)
    {
        if (index is null || index < 0 || index >= SavedTokens.Count)
        {
            return null;
        }

        return SavedTokens[index.Value];
    }

    /// <summary>
    /// Returns a copy with a token appended.
    /// </summary>
    /// <param name="token">The token value supplied by the user.</param>
    /// <param name="description">The description supplied by the user.</param>
    /// <returns>The updated settings.</returns>
    public ClientSettings AddSavedToken(string token, string description)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(description);
        var tokens = new List<SavedToken>(SavedTokens.Count + 1);
        tokens.AddRange(SavedTokens);
        tokens.Add(new SavedToken
        {
            Token = token,
            Description = description,
        });
        return this with
        {
            SavedTokens = tokens,
        };
    }

    /// <summary>
    /// Returns a copy with the token at <paramref name="index"/> removed.
    /// </summary>
    /// <param name="index">The index in <see cref="SavedTokens"/>.</param>
    /// <returns>The updated settings.</returns>
    public ClientSettings RemoveSavedTokenAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, SavedTokens.Count);
        var tokens = new List<SavedToken>(SavedTokens.Count - 1);
        for (var i = 0; i < SavedTokens.Count; i++)
        {
            if (i != index)
            {
                tokens.Add(SavedTokens[i]);
            }
        }

        return this with
        {
            SavedTokens = tokens,
        };
    }

    /// <summary>
    /// Gets the admin key and saved token values that must stay out of the call log.
    /// </summary>
    /// <returns>The non-empty stored secrets.</returns>
    public IReadOnlyList<string> SecretValues()
    {
        var secrets = new List<string>();
        if (AdminKey.Length > 0)
        {
            secrets.Add(AdminKey);
        }

        foreach (var saved in SavedTokens)
        {
            if (saved.Token.Length > 0)
            {
                secrets.Add(saved.Token);
            }
        }

        return secrets;
    }
}
