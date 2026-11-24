namespace AgentConnectClient.Calling;

/// <summary>
/// Removes stored token values and the admin key from text that is shown in the call log.
/// </summary>
public static class SecretRedactor
{
    /// <summary>
    /// The replacement used for a stored secret.
    /// </summary>
    public const string Mask = "***";

    /// <summary>
    /// Replaces each non-empty secret in <paramref name="text"/>. Longer secrets are replaced first.
    /// </summary>
    /// <param name="text">The text that may contain secrets.</param>
    /// <param name="secrets">The stored secret values.</param>
    /// <returns>The text with secrets replaced by <see cref="Mask"/>.</returns>
    public static string Redact(string text, IEnumerable<string> secrets)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(secrets);
        var ordered = new List<string>();
        foreach (var secret in secrets)
        {
            if (string.IsNullOrEmpty(secret))
            {
                continue;
            }

            var alreadyListed = false;
            foreach (var existing in ordered)
            {
                if (string.Equals(existing, secret, StringComparison.Ordinal))
                {
                    alreadyListed = true;
                    break;
                }
            }

            if (!alreadyListed)
            {
                ordered.Add(secret);
            }
        }

        ordered.Sort(static (left, right) => right.Length.CompareTo(left.Length));
        foreach (var secret in ordered)
        {
            text = text.Replace(secret, Mask, StringComparison.Ordinal);
        }

        return text;
    }
}
