namespace AgentConnectClient.Settings;

/// <summary>
/// A Member token stored with a description the user can select.
/// </summary>
public sealed record SavedToken
{
    /// <summary>
    /// Gets the token value. It is stored in plain text.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// Gets the description shown in the token selector.
    /// </summary>
    public required string Description { get; init; }
}
