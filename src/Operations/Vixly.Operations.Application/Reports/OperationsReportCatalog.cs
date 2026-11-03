namespace Vixly.Operations.Application.Reports;

// =========================================================
// FILE: OperationsReportCatalog.cs
//
// PURPOSE:
// Every report the company can run (Slice 11; owner, 2026-10-22: operations, finance, customers, carriers and warehouse).
//
// RESPONSIBILITIES:
// - List the reports with their columns and cuts; find one by key
//
// IMPORTANT NOTES:
// - Profit per shipment (Slice 8) is linked from the menu, not repeated here
// - A column or a report is only ever added (scheduled exports name them)
// =========================================================
public static class OperationsReportCatalog
{
    private static OperationsReportColumn C(string key) => new(key, OperationsReportValue.Count);

    private static OperationsReportColumn M(string key) => new(key, OperationsReportValue.Money);

    private static readonly OperationsReportDimension[] OverTime = { OperationsReportDimension.Period };

    private static readonly OperationsReportDimension[] ByWarehouse = { OperationsReportDimension.Warehouse };

    public static IReadOnlyList<OperationsReportDefinition> All { get; } = new[]
    {
        new OperationsReportDefinition("activity", OperationsReportGroup.Operations,
            new[] { C("received"), C("inspected"), C("holds_opened"), C("departed"), C("arrived"), C("ready"), C("picked_up"), C("delivered"), C("delivery_failed") }, OverTime),
        new OperationsReportDefinition("shipments", OperationsReportGroup.Operations,
            new[] { C("created"), C("booked"), C("parcels"), new OperationsReportColumn("weight", OperationsReportValue.Weight) }, OverTime, 1),
        new OperationsReportDefinition("dwell", OperationsReportGroup.Operations,
            new[] { C("departed"), new OperationsReportColumn("average_days", OperationsReportValue.Days), new OperationsReportColumn("median_days", OperationsReportValue.Days),
                new OperationsReportColumn("p90_days", OperationsReportValue.Days) }, ByWarehouse, 1),
        new OperationsReportDefinition("holds", OperationsReportGroup.Operations,
            new[] { C("opened"), C("released"), new OperationsReportColumn("average_hours", OperationsReportValue.Hours) }, Array.Empty<OperationsReportDimension>()),
        new OperationsReportDefinition("on_time", OperationsReportGroup.Operations,
            new[] { C("measured"), C("on_time"), C("late"), new OperationsReportColumn("on_time_percent", OperationsReportValue.Percent) }, Array.Empty<OperationsReportDimension>(), 3),
        new OperationsReportDefinition("sales", OperationsReportGroup.Finance,
            new[] { C("invoices"), M("subtotal"), M("tax"), M("total") },
            new[] { OperationsReportDimension.Period, OperationsReportDimension.Service, OperationsReportDimension.Route, OperationsReportDimension.Customer, OperationsReportDimension.Warehouse }, 3)
        { Finance = true },
        new OperationsReportDefinition("receivables", OperationsReportGroup.Finance,
            new[] { M("current"), M("days_1_30"), M("days_31_60"), M("days_61_90"), M("days_over_90"), M("owed") }, Array.Empty<OperationsReportDimension>(), 5)
        { Finance = true, AsOf = true },
        new OperationsReportDefinition("payables", OperationsReportGroup.Finance,
            new[] { M("current"), M("days_1_30"), M("days_31_60"), M("days_61_90"), M("days_over_90"), M("owed") }, Array.Empty<OperationsReportDimension>(), 5)
        { Finance = true, AsOf = true, CompanyWide = true },
        new OperationsReportDefinition("payments", OperationsReportGroup.Finance,
            new[] { C("payments"), M("amount"), M("pending") }, new[] { OperationsReportDimension.Method, OperationsReportDimension.Warehouse, OperationsReportDimension.Period }, 1)
        { Finance = true },
        new OperationsReportDefinition("taxes", OperationsReportGroup.Finance,
            new[] { M("basis"), M("tax"), M("credited"), M("net_tax") }, Array.Empty<OperationsReportDimension>(), 3)
        { Finance = true },
        new OperationsReportDefinition("signups", OperationsReportGroup.Customers, new[] { C("new_customers"), C("active_customers") }, OverTime) { CompanyWide = true },
        new OperationsReportDefinition("top_customers", OperationsReportGroup.Customers,
            new[] { C("shipments"), C("parcels"), new OperationsReportColumn("weight", OperationsReportValue.Weight), M("revenue") }, Array.Empty<OperationsReportDimension>(), 2),
        new OperationsReportDefinition("inactive_customers", OperationsReportGroup.Customers,
            new[] { C("shipments"), new OperationsReportColumn("days_since", OperationsReportValue.Days) }, Array.Empty<OperationsReportDimension>(), 1)
        { CompanyWide = true, AsOf = true, UsesDays = true },
        new OperationsReportDefinition("carrier_cost", OperationsReportGroup.Carriers,
            new[] { C("dispatches"), M("estimated"), M("billed"), M("difference") }, Array.Empty<OperationsReportDimension>(), 1)
        { Finance = true, CompanyWide = true },
        new OperationsReportDefinition("transit", OperationsReportGroup.Carriers,
            new[] { C("legs"), new OperationsReportColumn("average_hours", OperationsReportValue.Hours), new OperationsReportColumn("median_hours", OperationsReportValue.Hours) },
            Array.Empty<OperationsReportDimension>(), 1)
        { CompanyWide = true },
        new OperationsReportDefinition("stock_age", OperationsReportGroup.Carriers,
            new[] { C("on_hand"), C("days_0_7"), C("days_8_14"), C("days_15_30"), C("days_31_60"), C("days_over_60"), C("unclaimed") }, ByWarehouse)
        { AsOf = true, Now = true },
        new OperationsReportDefinition("supplies_value", OperationsReportGroup.Carriers, new[] { C("items"), C("units"), M("value") }, ByWarehouse, 2) { Finance = true, AsOf = true, Now = true }
    };

    public static OperationsReportDefinition? Find(string? key) => All.FirstOrDefault(report => string.Equals(report.Key, key, StringComparison.Ordinal));
}
