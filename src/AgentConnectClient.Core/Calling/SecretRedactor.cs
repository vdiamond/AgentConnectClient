using System.Text;

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
    /// A one-character secret is removed only when it stands alone, so letters inside ordinary words stay.
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

        var longSecrets = new List<string>();
        var shortSecrets = new List<char>();
        foreach (var secret in ordered)
        {
            if (secret.Length == 1)
            {
                shortSecrets.Add(secret[0]);
            }
            else
            {
                longSecrets.Add(secret);
            }
        }

        longSecrets.Sort(static (left, right) => right.Length.CompareTo(left.Length));
        foreach (var secret in longSecrets)
        {
            text = text.Replace(secret, Mask, StringComparison.Ordinal);
        }

        if (shortSecrets.Count == 0)
        {
            return text;
        }

        return RedactStandaloneCharacters(text, shortSecrets);
    }

    #region private

    private static string RedactStandaloneCharacters(string text, List<char> secrets)
    {
        var result = new StringBuilder(text.Length);
        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];
            var isSecret = false;
            foreach (var secret in secrets)
            {
                if (current == secret)
                {
                    isSecret = true;
                    break;
                }
            }

            if (isSecret && IsStandalone(text, index))
            {
                result.Append(Mask);
            }
            else
            {
                result.Append(current);
            }
        }

        return result.ToString();
    }

    private static bool IsStandalone(string text, int index)
    {
        var hasLetterOrDigitBefore = index > 0 && char.IsLetterOrDigit(text[index - 1]);
        var hasLetterOrDigitAfter = index + 1 < text.Length && char.IsLetterOrDigit(text[index + 1]);
        return !hasLetterOrDigitBefore && !hasLetterOrDigitAfter;
    }

    #endregion
}
