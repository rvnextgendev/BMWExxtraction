using BMW.ISTA.Core;
using Xunit;

namespace BMW.ISTA.Tests;

public class ModelTests
{
    [Theory]
    [InlineData("WMZ53GA05T7U74204", true)]
    [InlineData("WMW53GD00T2Y22769", true)]
    [InlineData("SHORT", false)]
    [InlineData("WMZ53GA05T7U7420I", false)]
    public void VinValidation(string vin, bool valid) => Assert.Equal(valid, InputValidation.IsVin(vin));

    [Theory]
    [InlineData("P0302", true)]
    [InlineData("U0123", true)]
    [InlineData("P03", false)]
    [InlineData("X0302", false)]
    public void DtcValidation(string dtc, bool valid) => Assert.Equal(valid, InputValidation.IsDtc(dtc));

    [Fact]
    public void JsonWritesToNestedDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "bmw-ista-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = Path.Combine(root, "nested", "record.json");
            LocalJson.Write(path, new ExtractionJob("job-1", null, "P0302", "pending", DateTimeOffset.UtcNow));
            var json = File.ReadAllText(path);
            Assert.Contains("\"faultCode\": \"P0302\"", json);
            Assert.Contains("\"status\": \"pending\"", json);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
