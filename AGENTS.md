# AGENTS.md

## What this repo is

AgentConnectClient is a Windows desktop app for acceptance-testing [AgentConnect](https://github.com/vdiamond/AgentConnect). The WPF app calls a fixed catalog of every AgentConnect v8 MCP tool (17) and every v8 REST endpoint (32). Choosing a call fills in example JSON. Running it shows the raw server result and an in-memory call log.

Status: this is the v1 client. `McpToolCaller` reports client version `1.0.0`. Main includes the v1.2 default server URLs and call-log redaction. There is no installer. The class library and tests are not a stub: they build and test without a live server. The v8 catalog is intentional. Do not retarget it to a later AgentConnect version unless Victor asks. Settings can still point at another host, including one you paste in for v9 or later.

The app runs on Windows (`net10.0-windows`). `AgentConnectClient.Core` and the tests target `net10.0` and run on Linux. The WPF project sets `EnableWindowsTargeting`, so it compiles on Linux and does not start there.

## Build, run, and test

`global.json` pins SDK `10.0.404` with `rollForward` `latestFeature`. Use that SDK or a later 10.0 SDK. A 10.0.1xx SDK, or a different major, does not satisfy the pin.

From the repository root:

```bash
dotnet build AgentConnectClient.slnx -c Release
```

```bash
dotnet test AgentConnectClient.slnx -c Release
```

Tests exist. They do not call a live AgentConnect server. REST and MCP are covered by the in-process stand-in in `tests/AgentConnectClient.Core.Tests/StandInServer.cs`.

Run the WPF app on Windows:

```bash
dotnet run --project src/AgentConnectClient.App/AgentConnectClient.App.csproj
```

On Linux that command builds the app and then exits 150. The apphost asks for `Microsoft.WindowsDesktop.App` 10.0.0, which is not installed here. Leave the target framework as `net10.0-windows`. There is no UI test project. Publish commands for a folder you can copy to Windows are in `README.md`.

## Layout

| Path | What it is |
|---|---|
| `AgentConnectClient.slnx` | Solution. Build and test this file. |
| `Directory.Build.props` | Shared `Nullable`, `ImplicitUsings`, `LangVersion` `latest`, `TreatWarningsAsErrors`, `Deterministic`. |
| `global.json` | SDK pin. |
| `.editorconfig` | Shared formatting. |
| `src/AgentConnectClient.Core` | `net10.0` library. Root namespace `AgentConnectClient`. Folders: `Calling/`, `Catalog/`, `Examples/`, `Settings/`, `Transport/`. `AgentConnectCallService.cs` sits at the project root. |
| `src/AgentConnectClient.App` | WPF screens (`App`, `MainWindow`, `CallScreen`, `SettingsScreen`). Project-references Core. |
| `tests/AgentConnectClient.Core.Tests` | xUnit. Project-references Core only. |

## Dependencies on other vdiamond repos

**[AgentConnect](https://github.com/vdiamond/AgentConnect)** is the only vdiamond repo this client depends on.

- Referenced at runtime over HTTP. Core sends REST calls and MCP tool calls (Streamable HTTP, official `ModelContextProtocol` package). There is no `ProjectReference`, git submodule, or project path into that repo.
- No sibling clone is required to build or test. Clone `https://github.com/vdiamond/AgentConnect` only when you want the server source. This repo does not read it from a neighboring folder.
- Projects: `AgentConnectClient.Core` performs the calls. `AgentConnectClient.App` uses Core. The tests stand in for the server and do not need the repo or a running host.
- The catalogs in `McpToolCatalog` and `RestEndpointCatalog` are the AgentConnect v8 lists (checked by `CatalogTests`), not a runtime discovery of whatever the server currently exposes.
- Built-in URLs, used only when the user has not saved settings yet, are the Azure prototype hosts in `DefaultServerUrls`.

No dependent vdiamond repos are named for this client.

NuGet packages already in the projects (do not add more unless Victor asks): `Microsoft.Extensions.Logging.Abstractions` 10.0.14 and `ModelContextProtocol` 2.4.0 on Core; `Microsoft.NET.Test.Sdk` 17.14.1, `xunit` 2.9.3, and `xunit.runner.visualstudio` 3.1.4 on the tests.

## Conventions

`.cursor/rules/coding-standards.mdc` is the governing standard. Existing code may not yet comply. Bring it into compliance when you touch it.

What the code does today:

- Nullable reference types, implicit usings, and the latest C# language version, from `Directory.Build.props`. Warnings are errors in every configuration, not only Release.
- File-scoped namespaces, braces on every block, and 4-space C# indentation (`.editorconfig`).
- `sealed` classes and `sealed record` types, `required` init properties, collection expressions, and `#region private` / `#region protected` member grouping.
- XML documentation on the Core public API (`GenerateDocumentationFile` is true there, false on the app and tests).
- `ConfigureAwait(false)` in Core. The WPF project uses `ConfigureAwait(true)`.
- `ArgumentNullException.ThrowIfNull` and the related throw helpers.
- xUnit tests named `MethodName_StateUnderTest_ExpectedBehavior`.
- Core's root namespace is `AgentConnectClient`, not `AgentConnectClient.Core`.

## What not to touch

- Do not commit secrets. The admin key and Member tokens are plain text in `%USERPROFILE%\AgentConnectClient\settings.json`, outside this repo, and they are unencrypted. Test fixtures use obvious stand-in values such as `admin-key` and `token-a`. Do not replace those with real credentials, and do not log stored secrets.
- Do not commit generated output: `bin/`, `obj/`, `TestResults/`, `.vs/`, `*.user`, `*.suo`, `.idea/`.
- Do not change the public API of `AgentConnectClient.Core` unless Victor asks. That includes catalog contents and order, settings JSON property names (camelCase), client error strings in `ClientErrorText`, and the call-service signatures.
- Do not retarget the catalogs off AgentConnect v8, add a package, or move the `global.json` pin unless Victor asks.
- Do not add `.grok/` instructions. Agent instructions are this file and `.cursor/rules/`.

Development happens only through Victor and Cursor agents.
