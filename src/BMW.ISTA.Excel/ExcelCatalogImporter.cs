using BMW.ISTA.Core;
using ExcelDataReader;
using System.Text;

namespace BMW.ISTA.Excel;

/// <summary>Streaming import: ExcelDataReader avoids loading huge formatted Excel dimensions into memory.</summary>
public static class ExcelCatalogImporter
{
    public static IReadOnlyList<VehicleRecord> ReadMiniVehicles(string path)
    {
        var vehicles = new List<VehicleRecord>();
        using var stream = File.OpenRead(path);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        do
        {
            if (!reader.Name.Equals("Sheet1", StringComparison.OrdinalIgnoreCase)) continue;
            while (reader.Read())
            {
                if (reader.FieldCount < 6) continue;
                var yearRaw = Cell(reader, 0);
                var vin = InputValidation.NormalizeVin(Cell(reader, 5));
                if (!int.TryParse(yearRaw, out var year) || !InputValidation.IsVin(vin)) continue;
                vehicles.Add(new VehicleRecord(year, Cell(reader, 1), Cell(reader, 2),
                    Cell(reader, 3), Cell(reader, 4), vin));
            }
            break;
        } while (reader.NextResult());
        return vehicles.DistinctBy(x => x.Vin).ToList();
    }

    public static IReadOnlyList<DtcRecord> ReadDtcCatalog(string path, ISet<string>? selectedCodes = null)
    {
        var dtcs = new List<DtcRecord>();
        using var stream = File.OpenRead(path);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        do
        {
            if (!reader.Name.StartsWith("P0000-U3FFF", StringComparison.OrdinalIgnoreCase)) continue;
            while (reader.Read())
            {
                if (reader.FieldCount < 4) continue;
                var code = InputValidation.NormalizeDtc(Cell(reader, 2));
                if (!InputValidation.IsDtc(code)) continue;
                if (selectedCodes is not null && !selectedCodes.Contains(code)) continue;
                dtcs.Add(new DtcRecord(code, Cell(reader, 3),
                    Optional(Cell(reader, 7)), Optional(Cell(reader, 8)), Optional(Cell(reader, 9))));
            }
            break;
        } while (reader.NextResult());
        return dtcs.DistinctBy(x => x.Code).ToList();
    }

    private static string Cell(IExcelDataReader reader, int column)
        => column < reader.FieldCount ? Convert.ToString(reader.GetValue(column))?.Trim() ?? "" : "";
    private static string? Optional(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
