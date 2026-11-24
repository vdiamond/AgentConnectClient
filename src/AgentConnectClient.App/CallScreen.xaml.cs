using System.Windows;
using System.Windows.Controls;
using AgentConnectClient.Calling;
using AgentConnectClient.Catalog;
using AgentConnectClient.Examples;
using AgentConnectClient.Settings;

namespace AgentConnectClient.App;

/// <summary>
/// The MCP or REST screen: catalog, token, editor, result, and call log.
/// </summary>
public partial class CallScreen : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CallScreen"/> class.
    /// </summary>
    public CallScreen()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when the user runs the selected call.
    /// </summary>
    public event EventHandler? RunRequested;

    /// <summary>
    /// Raised when the user saves a token from the result.
    /// </summary>
    public event EventHandler? SaveTokenRequested;

    /// <summary>
    /// Gets the surface this screen calls.
    /// </summary>
    public CallSurface Surface { get; private set; }

    /// <summary>
    /// Gets the selected saved-token index, or <see langword="null"/> when none is selected.
    /// </summary>
    public int? SelectedTokenIndex
    {
        get
        {
            if (TokenBox.SelectedIndex <= 0)
            {
                return null;
            }

            return TokenBox.SelectedIndex - 1;
        }
    }

    /// <summary>
    /// Gets the editor text.
    /// </summary>
    public string EditorText => EditorBox.Text ?? "";

    /// <summary>
    /// Gets the token value the user typed for saving a returned token.
    /// </summary>
    public string ReturnedToken => ReturnedTokenBox.Password ?? "";

    /// <summary>
    /// Gets the description the user typed for a returned token.
    /// </summary>
    public string ReturnedDescription => ReturnedDescriptionBox.Text ?? "";

    /// <summary>
    /// Gets the selected MCP tool name.
    /// </summary>
    public string? SelectedToolName
    {
        get
        {
            if (Surface != CallSurface.Mcp || CallList.SelectedItem is not string name)
            {
                return null;
            }

            return name;
        }
    }

    /// <summary>
    /// Gets the selected REST endpoint.
    /// </summary>
    public RestEndpointDefinition? SelectedEndpoint
    {
        get
        {
            if (Surface != CallSurface.Rest || CallList.SelectedItem is not string display)
            {
                return null;
            }

            foreach (var endpoint in RestEndpointCatalog.All)
            {
                if (string.Equals(endpoint.Display, display, StringComparison.Ordinal))
                {
                    return endpoint;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Fills the catalog for one surface.
    /// </summary>
    /// <param name="surface">MCP or REST.</param>
    public void Initialize(CallSurface surface)
    {
        Surface = surface;
        TitleText.Text = surface == CallSurface.Mcp ? "MCP" : "REST";
        CallList.Items.Clear();
        if (surface == CallSurface.Mcp)
        {
            foreach (var tool in McpToolCatalog.All)
            {
                CallList.Items.Add(tool.Name);
            }
        }
        else
        {
            foreach (var endpoint in RestEndpointCatalog.All)
            {
                CallList.Items.Add(endpoint.Display);
            }
        }
    }

    /// <summary>
    /// Refreshes the token selector and re-redacts the call log.
    /// </summary>
    /// <param name="tokens">The saved tokens.</param>
    /// <param name="secrets">The stored secret values used to redact the log.</param>
    /// <param name="selectIndex">The token to select, or <see langword="null"/> to keep the current selection when it is still valid.</param>
    public void SetTokens(IReadOnlyList<SavedToken> tokens, IReadOnlyList<string> secrets, int? selectIndex)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(secrets);
        var previous = selectIndex ?? SelectedTokenIndex;
        _secrets = secrets;
        TokenBox.Items.Clear();
        TokenBox.Items.Add("");
        foreach (var token in tokens)
        {
            TokenBox.Items.Add(token.Description);
        }

        if (previous is int index && index >= 0 && index < tokens.Count)
        {
            TokenBox.SelectedIndex = index + 1;
        }
        else
        {
            TokenBox.SelectedIndex = 0;
        }

        RenderLog(LogList.SelectedIndex);
    }

    /// <summary>
    /// Shows a finished call in the result pane and appends it to the log.
    /// </summary>
    /// <param name="entry">The call-log entry.</param>
    /// <param name="secrets">The stored secret values.</param>
    public void ShowResult(CallLogEntry entry, IReadOnlyList<string> secrets)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(secrets);
        _secrets = secrets;
        ResultBox.Text = entry.Result;
        _entries.Add(entry);
        RenderLog(_entries.Count - 1);
    }

    /// <summary>
    /// Enables or disables Run while a call is in flight.
    /// </summary>
    /// <param name="running">Whether a call is in flight.</param>
    public void SetRunning(bool running)
    {
        _running = running;
        RunButton.IsEnabled = !running && CallList.SelectedItem is not null;
    }

    /// <summary>
    /// Clears the save-token fields after the token is stored.
    /// </summary>
    public void ClearReturnedToken()
    {
        ReturnedTokenBox.Password = "";
        ReturnedDescriptionBox.Text = "";
    }

    #region private

    private readonly List<CallLogEntry> _entries = [];
    private IReadOnlyList<string> _secrets = [];
    private bool _running;

    private void CallList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CallList.SelectedItem is not string)
        {
            return;
        }

        if (Surface == CallSurface.Mcp && SelectedToolName is { } tool)
        {
            EditorBox.Text = ExampleJson.ForMcpTool(tool);
        }
        else if (SelectedEndpoint is { } endpoint)
        {
            EditorBox.Text = ExampleJson.ForRestEndpoint(endpoint.Method, endpoint.Path);
        }

        RunButton.IsEnabled = !_running;
    }

    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
        RunRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SaveTokenButton_Click(object sender, RoutedEventArgs e)
    {
        SaveTokenRequested?.Invoke(this, EventArgs.Empty);
    }

    private void LogList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LogList.SelectedIndex < 0 || LogList.SelectedIndex >= _entries.Count)
        {
            DetailBox.Text = "";
            return;
        }

        DetailBox.Text = _entries[LogList.SelectedIndex].ToLogDetail(_secrets);
    }

    private void RenderLog(int selectIndex)
    {
        EmptyLogText.Visibility = _entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        LogList.Items.Clear();
        foreach (var entry in _entries)
        {
            LogList.Items.Add(entry.ToDisplayLine(_secrets));
        }

        if (selectIndex >= 0 && selectIndex < _entries.Count)
        {
            LogList.SelectedIndex = selectIndex;
        }
    }

    #endregion
}
