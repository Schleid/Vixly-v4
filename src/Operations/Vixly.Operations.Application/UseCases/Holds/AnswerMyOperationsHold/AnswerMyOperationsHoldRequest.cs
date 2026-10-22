using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.UseCases.Holds.AnswerMyOperationsHold;

// =========================================================
// FILE: AnswerMyOperationsHoldRequest.cs
//
// PURPOSE:
// The customer answers a hold on their package: a document, the contents and value, or a choice for a damaged package (Slice 3E, Q-R7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AnswerMyOperationsHoldRequest
{
    public string? SessionToken { get; init; }

    public Guid ParcelId { get; init; }

    public OperationsHoldReasons Reason { get; init; }

    public string? Contents { get; init; }

    public decimal? Value { get; init; }

    public string? Choice { get; init; }

    public string? Note { get; init; }

    public byte[]? Document { get; init; }

    public string? FileName { get; init; }
}
