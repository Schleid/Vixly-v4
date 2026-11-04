namespace Vixly.Operations.Application.UseCases.Printing.RegisterOperationsPrintAgent;

// =========================================================
// FILE: RegisterOperationsPrintAgentRequest.cs
//
// PURPOSE:
// Registers the print program of a dock computer and gives its key once (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RegisterOperationsPrintAgentRequest
{
    public Guid WarehouseId { get; init; }

    public string? Name { get; init; }
}
