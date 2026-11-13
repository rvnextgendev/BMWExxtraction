using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BMW.ISTA.Core;

public sealed record VehicleRecord(int Year, string Make, string Model, string Engine, string FuelType, string Vin);
public sealed record DtcRecord(string Code, string Description, string? Format04Code, string? FailureTypeByte, string? FailureTypeDescription);
public sealed record ExtractionJob(string Id, string? Vin, string FaultCode, string Status, DateTimeOffset CreatedAtUtc);
public sealed record JobResult(string JobId, string Status, string? Error, DateTimeOffset RecordedAtUtc);
public sealed record ImportSummary(int VehicleCount, int DtcCount, int JobCount, DateTimeOffset CreatedAtUtc);

public static class InputValidation
{
    public static string NormalizeVin(string vin) => vin.Trim().ToUpperInvariant();
    public static string NormalizeDtc(string code) => code.Trim().ToUpperInvariant();
    public static bool IsVin(string value) => Regex.IsMatch(value, "^[A-HJ-NPR-Z0-9]{17}$");
    public static bool IsDtc(string value) => Regex.IsMatch(value, "^[PCBU][0-9A-F]{4}$");
}

public static class LocalJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static void Write<T>(string path, T value)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temp = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(value, Options));
            File.Move(temp, fullPath, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
