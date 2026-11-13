# BMW ISTA Extraction — Discovery Agent

A Windows-only **.NET 10 / C# / FlaUI** proof of concept for discovering UI Automation controls exposed by a locally installed BMW ISTA application.

> **Scope:** Read-only UI Automation discovery. It does not enter VIN/DTC values, click buttons, bypass login controls, or claim to extract diagnostic content from unknown ISTA screens. Run with authorization on your own installation. Review captured values before sharing, as they may contain VINs or other sensitive information.

## Prerequisites
- Windows 10/11 and .NET 10 SDK
- ISTA installed and already running in the same interactive desktop session
- Sufficient permission to inspect the ISTA process (normal user preferred; match privilege level if needed)
- Optional: FlaUInspect, to inspect selectors interactively

## Setup
```powershell
git clone https://github.com/rvnextgendev/BMWExxtraction.git
cd BMWExxtraction
dotnet restore BMWExxtraction.sln
dotnet build BMWExxtraction.sln
```

List windows:
```powershell
dotnet run --project src/BMW.ISTA.Discovery -- --list-windows
```

Capture by process ID:
```powershell
dotnet run --project src/BMW.ISTA.Discovery -- --pid 7716 --label results-P0302 --include-values
```

Or match the main window title:
```powershell
dotnet run --project src/BMW.ISTA.Discovery -- --title ISTA --label initial-scan
```

Output goes to `outputs/<label>-<UTC timestamp>.json` by default. Override with `--output C:\temp\ista.json`. Use `--max-depth 8` and `--max-nodes 3000` to limit capture; `--include-values` captures the text of ValuePattern where available, so keep it off unless required. `--help` shows all options.

## What the JSON contains
Metadata (capture timestamp, selected process ID, window title), an accessible control tree with Name, AutomationId, ClassName, ControlType, IsEnabled, BoundingRectangle, and optional ValuePattern text. Per-element failures are recorded as errors rather than aborting the entire capture.

## Roadmap
1. **Discovery (this repo):** map ISTA screens and export JSON without interacting.
2. **Selector mapping:** compare captures before and after manually opening VIN/DTC search results; identify reliable AutomationIds, labels, tables and navigation.
3. **Authorized input automation:** implement VIN/fault-code entry only after real selector evidence is provided; include explicit guards, waits, retries and human supervision.
4. **Data pipeline:** normalize extracted records to JSON first, then add SQLite mapping using an agreed schema; SQL Server export later.

## Safety and limitations
ISTA may use custom Chromium, WPF, Win32 or owner-drawn controls which are not visible to UIA. FlaUI cannot guarantee access to their content. Screens may include customer data or restricted/licensed BMW information; do not commit captured JSON. This repo deliberately excludes generated outputs and binaries.

## Troubleshooting
- If no window is found, start ISTA first, then run `--list-windows` and use its PID.
- If a window exposes few children, inspect with FlaUInspect and verify UIA3 vs legacy accessibility; some content cannot be obtained through UIA.
- The discovery agent never interacts with controls. Run it in the same Windows session and privilege level as ISTA.
- Dependency alerts: use current compatible packages and review any GitHub Dependabot alerts; avoid adding legacy `System.Drawing.Common` 5.0.2 explicitly.

## License
MIT. See LICENSE.
