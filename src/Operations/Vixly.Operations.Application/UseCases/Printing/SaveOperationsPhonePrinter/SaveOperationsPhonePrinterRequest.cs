using Vixly.Operations.Domain.Printing;
namespace Vixly.Operations.Application.UseCases.Printing.SaveOperationsPhonePrinter;

// =========================================================
// FILE: SaveOperationsPhonePrinterRequest.cs
//
// PURPOSE:
// A phone running Vixly Print adds or changes the portable printer next to it (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsPhonePrinterRequest
{
    public string AgentKey { get; init; } = string.Empty;

    public string? Name { get; init; }

    // The phone's identifier of the printer (Bluetooth).
    public string? Address { get; init; }

    public int Dpi { get; init; }

    public OperationsLabelMedia Media { get; init; }

    public bool Active { get; init; } = true;
}
