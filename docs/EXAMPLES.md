# Examples

## Import catalog and generate fault-code-only job
```powershell
dotnet run --project src/BMW.ISTA.Console -- --mini "C:\Data\MINI.xlsx" --dtc "C:\Data\DTC.xlsx" --code P0302 --out outputs/p0302
```

## Import and generate a vehicle-specific job
```powershell
dotnet run --project src/BMW.ISTA.Console -- --mini "C:\Data\MINI.xlsx" --dtc "C:\Data\DTC.xlsx" --code P0302 --vin WMZ53GA05T7U74204 --out outputs/p0302-vin
```

Files: `vehicles.json`, `dtcs.json`, `jobs.json`, `summary.json`. Generated jobs all have `status: pending`; they are **not** extraction results.

## Discover ISTA window
```powershell
dotnet run --project src/BMW.ISTA.Discovery -- --list-windows
dotnet run --project src/BMW.ISTA.Discovery -- --title ISTA --label results-screen
```
