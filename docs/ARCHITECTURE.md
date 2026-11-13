# Local-first architecture

```text
MINI Excel + DTC Excel
         |
         v
BMW.ISTA.Excel (streaming workbook readers)
         |
         v
BMW.ISTA.Core (VIN/DTC validation, records, atomic JSON)
         |
         v
BMW.ISTA.Console (filtered pending jobs + JSON catalogs)
         |
         v
[Future: ISTA automation after real selector discovery]
         |
         v
[Future: extraction results and SQLite/SQL persistence]
```

## Current supported scope

- The Discovery project captures a read-only UI Automation control tree from running ISTA.
- The Console project imports MINI model/VIN and DTC reference workbooks and generates *pending* jobs.
- The console does **not** invoke ISTA or claim extracted diagnostic data.
- The DTC catalog is a generic reference, not proof of a particular VIN/ISTA diagnosis.
- A VIN can be passed explicitly using `--vin`, or omitted for DTC-only jobs.
- DTC selections use `--code`; the reader streams the large workbook instead of materializing its huge formatted dimensions.
- `outputs/` is ignored by Git, as files may contain VINs or licensed reference data.

## Next

Run `BMW.ISTA.Discovery` with ISTA on relevant screens. Review the captured selectors (without publishing sensitive data), then add screen-specific drivers and navigation contracts. Only then implement real VIN/DTC searches, result extraction, safety guards, timeouts, and retry/idempotency. SQLite schema to be finalized later.
