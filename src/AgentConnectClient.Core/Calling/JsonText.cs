using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AgentConnectClient.Calling;

/// <summary>
/// Formats JSON for the result pane without changing string values.
/// </summary>
public static class JsonText
{
    /// <summary>
    /// Pretty-prints JSON. Text that is not JSON is returned unchanged.
    /// </summary>
    /// <param name="text">The raw text.</param>
    /// <returns>The pretty-printed JSON, or the original text.</returns>
    public static string Pretty(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        try
        {
            using var document = JsonDocument.Parse(text);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                document.WriteTo(writer);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return text;
        }
    }

    /// <summary>
    /// Builds the result-pane text. A status code is placed on the first line when the call has one.
    /// </summary>
    /// <param name="statusCode">The HTTP status code, or <see langword="null"/> for an MCP tool result.</param>
    /// <param name="body">The raw body or tool text.</param>
    /// <returns>The text shown in the result pane.</returns>
    public static string FormatForDisplay(int? statusCode, string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        var pretty = Pretty(body);
        if (statusCode is null)
        {
            return pretty;
        }

        return statusCode.Value.ToString(CultureInfo.InvariantCulture) + "\n" + pretty;
    }

    /// <summary>
    /// Reads the AgentConnect <c>error</c> property when the body is a JSON object.
    /// </summary>
    /// <param name="body">The raw body.</param>
    /// <returns>The error text, or <see langword="null"/> when the body has no string <c>error</c> property.</returns>
    public static string? ReadErrorProperty(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out var error)
                && error.ValueKind == JsonValueKind.String)
            {
                return error.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
