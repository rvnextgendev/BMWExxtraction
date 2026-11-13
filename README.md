# BMW ISTA Extraction — Local-first .NET 10

A Windows/.NET 10 BMW ISTA extraction project, built in stages. **The current modules import references, prepare jobs, and inspect ISTA controls. They do not yet perform automated ISTA searches or extract diagnostic results.**

## Projects
- `BMW.ISTA.Core` — VIN/DTC models, validation, atomic JSON storage
- `BMW.ISTA.Excel` — streaming MINI/VIN and generic DTC workbook import
- `BMW.ISTA.Console` — CLI to prepare filtered pending jobs and JSON files
- `BMW.ISTA.Discovery` — read-only FlaUI/UIA3 ISTA control-tree capture
- `BMW.ISTA.Tests` — input validation and JSON persistence tests

## Prerequisites
Windows 10/11, .NET 10 SDK, and running ISTA for UI discovery. The Excel reader also runs without ISTA.

```powershell
git clone https://github.com/rvnextgendev/BMWExxtraction.git
cd BMWExxtraction
dotnet restore BMWExxtraction.sln
dotnet build BMWExxtraction.sln
dotnet test BMWExxtraction.sln
```

## Step 1: Generate JSON using Excel input
The MINI workbook contains YEAR, MAKE, MODEL, ENGINE, TYPE and VIN. The DTC workbook has a worksheet named P0000-U3FFF with code in column C and description in column D. The importer reads these locally; do not upload source workbooks to this public repo.

```powershell
dotnet run --project src/BMW.ISTA.Console -- --mini "C:\Data\MINI.xlsx" --dtc "C:\Data\DTC.xlsx" --code P0302 --out outputs/p0302
```

For a VIN-specific pending job:
```powershell
dotnet run --project src/BMW.ISTA.Console -- --mini "C:\Data\MINI.xlsx" --dtc "C:\Data\DTC.xlsx" --code P0302 --vin WMZ53GA05T7U74204 --out outputs/p0302-mini
```

Writes `vehicles.json`, `dtcs.json`, `jobs.json` and `summary.json`. Jobs have `pending` status; they **are not ISTA extraction results**.

## Step 2: Inspect ISTA controls (Windows only)
```powershell
dotnet run --project src/BMW.ISTA.Discovery -- --list-windows
dotnet run --project src/BMW.ISTA.Discovery -- --pid 7716 --label initial --max-depth 8 --max-nodes 3000
```
Replace PID with actual ISTA process ID. Add `--include-values` only for authorized sensitive-text capture. Output: `outputs/<label>-<timestamp>.json`.

## Planned later
After verifying real UI automation selectors: read input jobs, navigate the ISTA fault-code/VIN search, capture result records and linked navigation, add resilient retries and explicit authorization controls; then add SQLite using an approved schema. Generic DTC definitions must not be misrepresented as ISTA-specific results.

See [architecture](docs/ARCHITECTURE.md) and [examples](docs/EXAMPLES.md).

## Privacy and security
Public repository; keep license-bound BMW content, VINs, workbooks and JSON outputs local. `outputs/` and `*.xlsx` are ignored. UI discovery may fail for custom controls. Do not attempt access beyond permitted applications or data.

MIT license.
