using BMW.ISTA.Core;
using BMW.ISTA.Excel;
using System.Text.Json;

try
{
    var options = Arguments.Parse(args);
    if (options.Help)
    {
        Console.WriteLine("""
        BMW ISTA input preparation (local only; does NOT operate ISTA)
        dotnet run --project src/BMW.ISTA.Console -- --mini "MINI.xlsx" --dtc "DTC.xlsx" --code P0302 --out outputs/batch
        Options: --mini <xlsx>  --dtc <xlsx>  --code <DTC> (repeat)
                 --vin <VIN> (repeat) --out <directory> --help
        Requires at least one --code to create jobs. Use --vin to narrow vehicle scope.
        Without --vin, jobs are DTC-only (no arbitrary VIN x DTC cartesian product).
        """);
        return;
    }

    Directory.CreateDirectory(options.Output);
    var vehicles = options.Mini is null ? Array.Empty<VehicleRecord>() :
        ExcelCatalogImporter.ReadMiniVehicles(options.Mini).ToArray();
    var codes = options.Codes.Count > 0 ? options.Codes : null;
    var dtcs = options.Dtc is null ? Array.Empty<DtcRecord>() :
        ExcelCatalogImporter.ReadDtcCatalog(options.Dtc, codes).ToArray();

    var selectedVins = options.Vins;
    var jobs = new List<ExtractionJob>();
    foreach (var code in options.Codes.OrderBy(x => x))
    {
        if (selectedVins.Count == 0)
        {
            jobs.Add(new ExtractionJob(Guid.NewGuid().ToString("N"), null, code, "pending", DateTimeOffset.UtcNow));
        }
        else
        {
            foreach (var vin in selectedVins.OrderBy(x => x))
                jobs.Add(new ExtractionJob(Guid.NewGuid().ToString("N"), vin, code, "pending", DateTimeOffset.UtcNow));
        }
    }
    LocalJson.Write(Path.Combine(options.Output, "vehicles.json"), vehicles);
    LocalJson.Write(Path.Combine(options.Output, "dtcs.json"), dtcs);
    LocalJson.Write(Path.Combine(options.Output, "jobs.json"), jobs);
    LocalJson.Write(Path.Combine(options.Output, "summary.json"),
        new ImportSummary(vehicles.Length, dtcs.Length, jobs.Count, DateTimeOffset.UtcNow));
    Console.WriteLine($"Saved JSON in {Path.GetFullPath(options.Output)}: {vehicles.Length} vehicles, {dtcs.Length} DTCs, {jobs.Count} pending jobs.");
    Console.WriteLine("No ISTA actions were performed.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERROR: {ex.Message}");
    Environment.ExitCode = 1;
}

internal sealed record Arguments(bool Help, string? Mini, string? Dtc, string Output,
    HashSet<string> Codes, HashSet<string> Vins)
{
    public static Arguments Parse(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help"))
            return new(true, null, null, "outputs/import", new(), new());
        string? mini = null, dtc = null;
        var output = "outputs/import";
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var vins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            string Next()
            {
                if (++i >= args.Length) throw new ArgumentException($"Missing value for {args[i - 1]}");
                return args[i];
            }
            switch (args[i])
            {
                case "--mini": mini = Next(); break;
                case "--dtc": dtc = Next(); break;
                case "--out": output = Next(); break;
                case "--code":
                    var code = InputValidation.NormalizeDtc(Next());
                    if (!InputValidation.IsDtc(code)) throw new ArgumentException($"Invalid DTC: {code}");
                    codes.Add(code);
                    break;
                case "--vin":
                    var vin = InputValidation.NormalizeVin(Next());
                    if (!InputValidation.IsVin(vin)) throw new ArgumentException($"Invalid VIN: {vin}");
                    vins.Add(vin);
                    break;
                default: throw new ArgumentException($"Unknown option: {args[i]}");
            }
        }
        if (mini is null && dtc is null) throw new ArgumentException("Supply --mini and/or --dtc.");
        if (mini is not null && !File.Exists(mini)) throw new FileNotFoundException("MINI workbook not found", mini);
        if (dtc is not null && !File.Exists(dtc)) throw new FileNotFoundException("DTC workbook not found", dtc);
        return new(false, mini, dtc, output, codes, vins);
    }
}
