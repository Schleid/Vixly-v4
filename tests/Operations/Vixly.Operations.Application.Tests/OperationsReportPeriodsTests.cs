using Vixly.Operations.Application.Reports;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsReportPeriodsTests.cs
//
// PURPOSE:
// Proves the report periods and the CSV writer (Slice 11).
//
// RESPONSIBILITIES:
// - At most 13 months; the period before; weeks from Monday; every cut listed; CSV cells guarded
//
// IMPORTANT NOTES:
// - Pure; no database
// =========================================================
public sealed class OperationsReportPeriodsTests
{
    [Fact]
    public void Period_IsAtMostThirteenMonths_AndComparedWithTheOneBefore()
    {
        DateOnly from = new(2026, 1, 1);
        Assert.True(OperationsReportPeriods.Valid(from, new DateOnly(2027, 1, 31)));
        Assert.False(OperationsReportPeriods.Valid(from, new DateOnly(2027, 2, 1)));
        Assert.False(OperationsReportPeriods.Valid(from, from.AddDays(-1)));
        Assert.Equal((new DateOnly(2025, 12, 22), new DateOnly(2025, 12, 31)), OperationsReportPeriods.Before(from, new DateOnly(2026, 1, 10)));
    }

    [Fact]
    public void Cuts_StartOnMondayOrTheFirst_AndCoverEveryDay()
    {
        Assert.Equal(new DateOnly(2026, 10, 19), OperationsReportPeriods.Start(new DateOnly(2026, 10, 25), OperationsReportGrain.Week));
        Assert.Equal(new DateOnly(2026, 10, 19), OperationsReportPeriods.Start(new DateOnly(2026, 10, 19), OperationsReportGrain.Week));
        Assert.Equal(new DateOnly(2026, 10, 1), OperationsReportPeriods.Start(new DateOnly(2026, 10, 25), OperationsReportGrain.Month));
        Assert.Equal(3, OperationsReportPeriods.Cuts(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 3), OperationsReportGrain.Day).Count);
        Assert.Equal(new[] { new DateOnly(2026, 9, 28), new DateOnly(2026, 10, 5) },
            OperationsReportPeriods.Cuts(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 6), OperationsReportGrain.Week).ToArray());
        Assert.Equal(13, OperationsReportPeriods.Cuts(new DateOnly(2026, 1, 15), new DateOnly(2027, 1, 14), OperationsReportGrain.Month).Count);
    }

    [Fact]
    public void Csv_GuardsCellsThatLookLikeFormulas_AndKeepsNumbersPlain()
    {
        OperationsReportSheet sheet = new("Sales", Array.Empty<string>(), new[] { "Item", "Total" },
            new[] { (IReadOnlyList<OperationsReportCell>)new[] { new OperationsReportCell("=HYPERLINK(\"x\")"), new OperationsReportCell("1,234.50 USD", 1234.5m) } });
        string csv = System.Text.Encoding.UTF8.GetString(OperationsReportCsv.Write(sheet)).TrimStart('\uFEFF');
        Assert.Equal("\"Item\",\"Total\"\r\n\"'=HYPERLINK(\"\"x\"\")\",1234.5\r\n", csv);
    }
}
