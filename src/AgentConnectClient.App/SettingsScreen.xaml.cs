using System.Windows;
using System.Windows.Controls;
using AgentConnectClient.Calling;
using AgentConnectClient.Settings;

namespace AgentConnectClient.App;

/// <summary>
/// Settings for the MCP URL, REST URL, admin key, and saved tokens.
/// </summary>
public partial class SettingsScreen : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsScreen"/> class.
    /// </summary>
    public SettingsScreen()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when the user saves settings.
    /// </summary>
    public event EventHandler? SaveRequested;

    /// <summary>
    /// Loads the committed settings into the form.
    /// </summary>
    /// <param name="settings">The settings to show.</param>
    /// <param name="filePath">The settings file path.</param>
    public void LoadFrom(ClientSettings settings, string filePath)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        McpUrlBox.Text = settings.McpServerUrl;
        RestUrlBox.Text = settings.RestBaseUrl;
        AdminKeyBox.Password = settings.AdminKey;
        _draft = new List<SavedToken>(settings.SavedTokens);
        RefreshList();
        FilePathText.Text = filePath;
        StatusText.Text = "";
    }

    /// <summary>
    /// Reads the form, including tokens that have not been written yet.
    /// </summary>
    /// <returns>The settings shown on the form.</returns>
    public ClientSettings ReadDraft()
    {
        return new ClientSettings
        {
            McpServerUrl = McpUrlBox.Text ?? "",
            RestBaseUrl = RestUrlBox.Text ?? "",
            AdminKey = AdminKeyBox.Password ?? "",
            SavedTokens = _draft.ToArray(),
        };
    }

    /// <summary>
    /// Adds a token that was saved from a call result so the form matches the file.
    /// </summary>
    /// <param name="token">The token value.</param>
    /// <param name="description">The description.</param>
    public void AppendToken(string token, string description)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(description);
        _draft.Add(new SavedToken
        {
            Token = token,
            Description = description,
        });
        RefreshList();
    }

    /// <summary>
    /// Shows that the file was written.
    /// </summary>
    public void MarkSaved()
    {
        StatusText.Text = "Saved";
    }

    /// <summary>
    /// Shows a save error. The message must not include secret values.
    /// </summary>
    /// <param name="message">The error text.</param>
    public void ShowError(string message)
    {
        StatusText.Text = message;
    }

    #region private

    private List<SavedToken> _draft = [];

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var token = NewTokenBox.Password ?? "";
        var description = NewDescriptionBox.Text ?? "";
        if (token.Length == 0)
        {
            return;
        }

        _draft.Add(new SavedToken
        {
            Token = token,
            Description = description,
        });
        NewTokenBox.Password = "";
        NewDescriptionBox.Text = "";
        StatusText.Text = "";
        RefreshList();
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        var index = TokenList.SelectedIndex;
        if (index < 0 || index >= _draft.Count)
        {
            return;
        }

        _draft.RemoveAt(index);
        StatusText.Text = "";
        RefreshList();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SaveRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UrlBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var cleartext = CleartextHttp.IsHttp(McpUrlBox.Text) || CleartextHttp.IsHttp(RestUrlBox.Text);
        UrlWarningText.Text = cleartext ? CleartextHttp.Warning : "";
        UrlWarningText.Visibility = cleartext ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshList()
    {
        TokenList.Items.Clear();
        foreach (var token in _draft)
        {
            TokenList.Items.Add(token.Description);
        }
    }

    #endregion
}
