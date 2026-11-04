using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Reports;

// =========================================================
// FILE: OperationsReportErrors.cs
//
// PURPOSE:
// Error codes of the reports (Slice 11).
//
// RESPONSIBILITIES:
// - Name each refusal
//
// IMPORTANT NOTES:
// - Codes only; texts in OperationsText (en, fr, es)
// =========================================================
public static class OperationsReportErrors
{
    public static Error NotFound { get; } = Error.Create("ops.reports.not_found");

    public static Error PeriodInvalid { get; } = Error.Create("ops.reports.period_invalid");

    public static Error RulesInvalid { get; } = Error.Create("ops.reports.rules_invalid");

    public static Error WarehouseNotAllowed { get; } = Error.Create("ops.reports.warehouse_not_allowed");

    public static Error RecipientsInvalid { get; } = Error.Create("ops.reports.recipients_invalid");

    public static Error ScheduleNotFound { get; } = Error.Create("ops.reports.schedule_not_found");

    public static Error TooManySchedules { get; } = Error.Create("ops.reports.too_many_schedules");
}
