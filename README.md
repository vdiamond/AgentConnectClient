# AgentConnectClient

Windows desktop app for acceptance-testing AgentConnect. It calls every AgentConnect v8 MCP tool and every v8 REST endpoint. Picking a call fills in example JSON you can edit. Running it shows the raw result and a call log.

The app runs on Windows. The class library and its tests target `net10.0` and run on Linux. The WPF project targets `net10.0-windows` and sets `EnableWindowsTargeting`, so it also compiles on Linux.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build
- Windows and the .NET 10 desktop runtime to run a framework-dependent build

## Build

From the repository root:

```bash
dotnet build AgentConnectClient.slnx -c Release
```

Run the tests (no live AgentConnect server; REST and MCP are covered with an in-process stand-in):

```bash
dotnet test AgentConnectClient.slnx -c Release
```

## Run on Windows

On a first run, and when the settings file does not exist yet, the MCP server URL and REST base URL are the built-in prototype defaults:

- MCP: `https://ac-mcp.greenpond-2de37f99.eastus.azurecontainerapps.io/mcp`
- REST: `https://ac-api.greenpond-2de37f99.eastus.azurecontainerapps.io`

URLs you have already saved are left as they are. Clearing a URL and choosing **Save** keeps it empty. Choose **Settings** to change the URLs, set the admin key, add Member tokens, and choose **Save**. Against AgentConnect v8, tokens arrive by email and are pasted into Settings. A result that contains a plaintext token (AgentConnect v9 or later) can be saved from the result with **Save token**. A live v9 host is not a built-in default; paste it in Settings if you want to use one.

```bash
dotnet run --project src/AgentConnectClient.App/AgentConnectClient.App.csproj
```

Settings are plain text at `%USERPROFILE%\AgentConnectClient\settings.json`. Token values and the admin key are stored unencrypted. Each Windows user has a separate file. The call log is kept in memory for the current session only and does not include those secret values.

A saved token is sent as `Authorization: Bearer <token>`. Paste the token value itself, without the `Bearer` prefix. Platform-operator routes (`POST /tenants`, send-invitation, suspend, resume, and `DELETE /tenants/{id}`) send `X-Admin-Key` and do not send a Member token. An `http://` URL is accepted and shows a warning, because the admin key and tokens then travel in clear text. Redirects are not followed.

## Publish a folder you can copy to Windows

Framework-dependent (the machine needs the .NET 10 desktop runtime):

```bash
dotnet publish src/AgentConnectClient.App/AgentConnectClient.App.csproj -c Release -r win-x64 --self-contained false
```

Self-contained (larger folder, no separate runtime install):

```bash
dotnet publish src/AgentConnectClient.App/AgentConnectClient.App.csproj -c Release -r win-x64 --self-contained true
```

The app is the `AgentConnectClient.App.exe` in the publish folder:

`src/AgentConnectClient.App/bin/Release/net10.0-windows/win-x64/publish/`

Copy that folder to the Windows machine and run the exe. v1 does not ship an installer.

## Projects

| Project | Purpose |
|---|---|
| `src/AgentConnectClient.Core` | Catalogs, example JSON, settings file, header selection, URL joining, and the MCP and REST callers |
| `src/AgentConnectClient.App` | WPF screens |
| `tests/AgentConnectClient.Core.Tests` | Unit tests and stand-in integration tests |

## Notes

- MCP calls use the official C# SDK over Streamable HTTP. Each run opens a client and closes it. The app does not keep a session or a long-lived event stream, and it does not set a call timeout.
- The SDK may send `server/discover` (and fall back to `initialize` when the server requires it) before `tools/call`. That handshake is how the SDK speaks stateless Streamable HTTP.
- `name` and `description` on `update_workspace`, `update_list`, `PATCH /workspaces/{id}`, and `PATCH /workspaces/{id}/lists/{listId}` are placeholder strings. On those calls, JSON null is a real change.
- Editor text that is valid JSON but not an object is reported as `Request is not valid JSON`.
- The result pane shows the server result unchanged, including a token the server returned. The call log redacts stored token values and the admin key. Choose **Save token**, paste the value from the result, enter a description, and the new token is written to the settings file and can be selected on the next call.
- Settings changes are written when you choose **Save**. **Remove** deletes a saved token from the list; choose **Save** to keep that change.
