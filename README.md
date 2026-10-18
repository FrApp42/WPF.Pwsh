# WPF.Pwsh

A WPF control that hosts a real PowerShell session (ConPTY) — the shell starts with the control, renders
through the Windows Terminal control, and raises an event when it exits.

```xml
<PackageReference Include="FrApp42.WPF.Pwsh" Version="1.0.0" />
```

```csharp
var terminal = new FrApp42.WPF.Pwsh.PwshTerminal();     // pwsh.exe, powershell.exe as fallback
terminal.OnSessionDisconnected += (_, _) => CloseTab(); // the shell exited
Content = terminal;
```

- `new PwshTerminal("wsl.exe")` runs any other shell.
- `WorkingDirectory` sets where it starts.
- `FontFamily` / `FontSize` set the terminal font (default `MesloLGS Nerd Font Mono`, 14).
- `Disconnect()` kills the shell, `Dispose()` tears the session down.

Windows only, net8.0-windows / net10.0-windows.

## Publishing

`Publish` (`.github/workflows/publish.yml`) packs and pushes to nuget.org through Trusted Publishing — no API key
stored. Declare the policy on nuget.org (owner `FrApp42`, repository `WPF.Pwsh`, workflow file `publish.yml`), set the
repository secret `NUGET_USER` to your nuget.org profile name, bump `<Version>` in the csproj, then publish a release.
