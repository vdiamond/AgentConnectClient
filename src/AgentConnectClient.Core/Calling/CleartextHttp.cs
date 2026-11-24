namespace AgentConnectClient.Calling;

/// <summary>
/// Detects an http URL, which sends the admin key and tokens in clear text.
/// </summary>
public static class CleartextHttp
{
    /// <summary>
    /// The warning shown when a saved or typed URL uses http.
    /// </summary>
    public const string Warning = "An http:// URL sends the admin key and tokens in clear text.";

    /// <summary>
    /// Returns whether <paramref name="url"/> is an absolute http URL.
    /// </summary>
    /// <param name="url">The URL as entered.</param>
    /// <returns><see langword="true"/> when the scheme is http.</returns>
    public static bool IsHttp(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsed)
            && string.Equals(parsed.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
    }
}
