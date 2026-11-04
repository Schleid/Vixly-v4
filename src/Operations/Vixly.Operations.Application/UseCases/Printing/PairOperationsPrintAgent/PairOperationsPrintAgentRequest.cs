namespace Vixly.Operations.Application.UseCases.Printing.PairOperationsPrintAgent;

// =========================================================
// FILE: PairOperationsPrintAgentRequest.cs
//
// PURPOSE:
// A phone running Vixly Print joins a warehouse with its one-time code and receives its print program key (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PairOperationsPrintAgentRequest
{
    public string? Code { get; init; }

    // What the phone calls itself, added to the name given on the Printers page.
    public string? DeviceName { get; init; }
}
