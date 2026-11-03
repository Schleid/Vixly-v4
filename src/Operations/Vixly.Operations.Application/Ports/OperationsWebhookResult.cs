namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsWebhookResult.cs
//
// PURPOSE:
// What came of posting a webhook (Slice 10).
//
// RESPONSIBILITIES:
// - Carry the status the receiver answered, or a short reason when none came
//
// IMPORTANT NOTES:
// - Delivered means a 2xx answer
// =========================================================
public sealed record OperationsWebhookResult(int? StatusCode, string? Error)
{
    public bool Delivered => StatusCode is >= 200 and < 300;
}
