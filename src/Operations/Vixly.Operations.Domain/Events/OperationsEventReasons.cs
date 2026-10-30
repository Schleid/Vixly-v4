namespace Vixly.Operations.Domain.Events;

// =========================================================
// FILE: OperationsEventReasons.cs
//
// PURPOSE:
// The standard reasons for delays and exceptions every company has (Slice 6C, E-3).
//
// RESPONSIBILITIES:
// - List the standard codes per kind
// - Say whether a code is standard, and check a company reason's code
//
// IMPORTANT NOTES:
// - Worded by ui.event_reason.<code> in EN, FR and ES
// - A company switches standard ones off and adds its own (OperationsEventReason)
// =========================================================
public static class OperationsEventReasons
{
    public const string CustomPrefix = "c_";

    public static IReadOnlyList<string> Delay { get; } = new[]
    {
        "weather", "flight_rebooked", "missed_connection", "carrier_delay", "capacity", "customs_inspection", "documents_pending", "holiday"
    };

    public static IReadOnlyList<string> Exception { get; } = new[]
    {
        "damaged", "address_problem", "recipient_unreachable", "customs_hold", "documents_missing", "refused", "security_check", "missing_in_transit"
    };

    public static IReadOnlyList<string> For(OperationsEventKind kind) => kind switch
    {
        OperationsEventKind.Delay => Delay,
        OperationsEventKind.Exception => Exception,
        _ => Array.Empty<string>()
    };

    public static bool IsStandard(string? code) => code is not null && (Delay.Contains(code, StringComparer.Ordinal) || Exception.Contains(code, StringComparer.Ordinal));
}
