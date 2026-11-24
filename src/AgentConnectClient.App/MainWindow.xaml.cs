using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgentConnectClient.Calling;
using AgentConnectClient.Settings;

namespace AgentConnectClient.App;

/// <summary>
/// Hosts the MCP screen, the REST screen, and Settings.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        _store = new FileSettingsStore();
        _calls = new AgentConnectCallService();
        _settings = new ClientSettings();
        McpScreen.Initialize(CallSurface.Mcp);
        RestScreen.Initialize(CallSurface.Rest);
        McpScreen.RunRequested += OnMcpRun;
        RestScreen.RunRequested += OnRestRun;
        McpScreen.SaveTokenRequested += OnMcpSaveToken;
        RestScreen.SaveTokenRequested += OnRestSaveToken;
        SettingsView.SaveRequested += OnSettingsSave;
        Loaded += OnLoaded;
        Closed += OnClosed;
        ShowScreen(McpScreen);
    }

    #region private

    private readonly FileSettingsStore _store;
    private readonly AgentConnectCallService _calls;
    private ClientSettings _settings;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings = await _store.LoadAsync().ConfigureAwait(true);
        }
        catch (SettingsFileException ex)
        {
            _settings = new ClientSettings();
            MessageBox.Show(this, ex.Message, "Settings");
        }

        SettingsView.LoadFrom(_settings, _store.FilePath);
        RefreshTokens(null);
        ShowScreen(McpScreen);
    }

    private async void OnMcpRun(object? sender, EventArgs e)
    {
        await RunAsync(McpScreen).ConfigureAwait(true);
    }

    private async void OnRestRun(object? sender, EventArgs e)
    {
        await RunAsync(RestScreen).ConfigureAwait(true);
    }

    private async void OnMcpSaveToken(object? sender, EventArgs e)
    {
        await SaveReturnedTokenAsync(McpScreen).ConfigureAwait(true);
    }

    private async void OnRestSaveToken(object? sender, EventArgs e)
    {
        await SaveReturnedTokenAsync(RestScreen).ConfigureAwait(true);
    }

    private async void OnSettingsSave(object? sender, EventArgs e)
    {
        try
        {
            _settings = SettingsView.ReadDraft();
            await _store.SaveAsync(_settings).ConfigureAwait(true);
            RefreshTokens(null);
            SettingsView.MarkSaved();
        }
        catch (SettingsFileException ex)
        {
            SettingsView.ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            SettingsView.ShowError(ex.GetType().Name);
        }
    }

    private async Task RunAsync(CallScreen screen)
    {
        screen.SetRunning(true);
        try
        {
            CallLogEntry entry;
            if (screen.Surface == CallSurface.Mcp)
            {
                var tool = screen.SelectedToolName;
                if (tool is null)
                {
                    return;
                }

                entry = await _calls.RunMcpAsync(_settings, screen.SelectedTokenIndex, tool, screen.EditorText).ConfigureAwait(true);
            }
            else
            {
                var endpoint = screen.SelectedEndpoint;
                if (endpoint is null)
                {
                    return;
                }

                entry = await _calls.RunRestAsync(
                    _settings,
                    screen.SelectedTokenIndex,
                    endpoint.Method,
                    endpoint.Path,
                    screen.EditorText).ConfigureAwait(true);
            }

            if (!Dispatcher.HasShutdownStarted)
            {
                screen.ShowResult(entry, _settings.SecretValues());
            }
        }
        catch (Exception ex)
        {
            if (Dispatcher.HasShutdownStarted)
            {
                return;
            }

            var message = SecretRedactor.Redact(ex.Message, _settings.SecretValues());
            screen.ShowResult(new CallLogEntry
            {
                Time = DateTime.Now,
                Surface = screen.Surface,
                Name = screen.Surface == CallSurface.Mcp ? screen.SelectedToolName ?? "MCP" : screen.SelectedEndpoint?.Display ?? "REST",
                Request = screen.EditorText,
                Result = message,
                ClientError = true,
                Succeeded = false,
                ErrorText = message,
            }, _settings.SecretValues());
        }
        finally
        {
            if (!Dispatcher.HasShutdownStarted)
            {
                screen.SetRunning(false);
            }
        }
    }

    private async Task SaveReturnedTokenAsync(CallScreen screen)
    {
        var token = screen.ReturnedToken;
        var description = screen.ReturnedDescription;
        if (token.Length == 0)
        {
            return;
        }

        try
        {
            _settings = _settings.AddSavedToken(token, description);
            await _store.SaveAsync(_settings).ConfigureAwait(true);
            SettingsView.AppendToken(token, description);
            var index = _settings.SavedTokens.Count - 1;
            var secrets = _settings.SecretValues();
            screen.SetTokens(_settings.SavedTokens, secrets, index);
            var other = ReferenceEquals(screen, McpScreen) ? RestScreen : McpScreen;
            other.SetTokens(_settings.SavedTokens, secrets, null);
            screen.ClearReturnedToken();
        }
        catch (SettingsFileException ex)
        {
            MessageBox.Show(this, ex.Message, "Settings");
        }
    }

    private void RefreshTokens(int? selectIndex)
    {
        var secrets = _settings.SecretValues();
        McpScreen.SetTokens(_settings.SavedTokens, secrets, selectIndex);
        RestScreen.SetTokens(_settings.SavedTokens, secrets, selectIndex);
        McpScreen.SetCleartextWarning(CleartextHttp.IsHttp(_settings.McpServerUrl));
        RestScreen.SetCleartextWarning(CleartextHttp.IsHttp(_settings.RestBaseUrl));
    }

    private void ShowMcp_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen(McpScreen);
    }

    private void ShowRest_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen(RestScreen);
    }

    private void ShowSettings_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen(null);
    }

    private void ShowScreen(CallScreen? screen)
    {
        McpScreen.Visibility = ReferenceEquals(screen, McpScreen) ? Visibility.Visible : Visibility.Collapsed;
        RestScreen.Visibility = ReferenceEquals(screen, RestScreen) ? Visibility.Visible : Visibility.Collapsed;
        SettingsView.Visibility = screen is null ? Visibility.Visible : Visibility.Collapsed;
        MarkNav(McpButton, ReferenceEquals(screen, McpScreen));
        MarkNav(RestButton, ReferenceEquals(screen, RestScreen));
        MarkNav(SettingsButton, screen is null);
    }

    private static void MarkNav(Button button, bool selected)
    {
        if (selected)
        {
            button.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            button.Foreground = Brushes.White;
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
        }
        else
        {
            button.Background = new SolidColorBrush(Color.FromRgb(229, 231, 235));
            button.Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 51));
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219));
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _calls.Dispose();
    }

    #endregion
}
