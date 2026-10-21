namespace Vixly.Operations.Application.UseCases.Receiving.RecordOperationsParcelLabel;

// =========================================================
// FILE: RecordOperationsParcelLabelRequest.cs
//
// PURPOSE:
// Records what the label of an undeclared package shows: the name and mailbox code written on it, the sender (Slice 4D step 3, design section 6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RecordOperationsParcelLabelRequest
{
    public Guid ParcelId { get; init; }

    public string? Name { get; init; }

    public string? Code { get; init; }

    public string? Sender { get; init; }
}
