using System.Text;

namespace AgentConnectClient.Calling;

/// <summary>
/// Joins the REST base URL, path parameters, and query parameters.
/// </summary>
public static class RestUrlComposer
{
    /// <summary>
    /// Builds the request URL. A trailing slash on the base URL does not produce a double slash.
    /// </summary>
    /// <param name="baseUrl">The REST base URL.</param>
    /// <param name="pathTemplate">The path template from the catalog.</param>
    /// <param name="pathValues">The substituted path values, already validated.</param>
    /// <param name="query">The query parameters to send. Null query values are omitted by the caller.</param>
    /// <returns>The absolute request URL text.</returns>
    public static string Compose(
        string baseUrl,
        string pathTemplate,
        IReadOnlyDictionary<string, string> pathValues,
        IReadOnlyList<KeyValuePair<string, string>> query)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseUrl);
        ArgumentException.ThrowIfNullOrEmpty(pathTemplate);
        ArgumentNullException.ThrowIfNull(pathValues);
        ArgumentNullException.ThrowIfNull(query);

        var path = pathTemplate;
        foreach (var pair in pathValues)
        {
            path = path.Replace("{" + pair.Key + "}", Uri.EscapeDataString(pair.Value), StringComparison.Ordinal);
        }

        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        var combined = baseUrl.TrimEnd('/') + path;
        if (query.Count == 0)
        {
            return combined;
        }

        var builder = new StringBuilder(combined);
        builder.Append('?');
        for (var index = 0; index < query.Count; index++)
        {
            if (index > 0)
            {
                builder.Append('&');
            }

            builder.Append(Uri.EscapeDataString(query[index].Key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(query[index].Value));
        }

        return builder.ToString();
    }
}
