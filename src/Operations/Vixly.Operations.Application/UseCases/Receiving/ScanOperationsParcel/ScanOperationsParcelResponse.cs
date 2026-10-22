using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.ScanOperationsParcel;

// =========================================================
// FILE: ScanOperationsParcelResponse.cs
//
// PURPOSE:
// Receives one package at the dock by its carrier barcode and prints its ID label (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ScanOperationsParcelResponse
{
    private ScanOperationsParcelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ParcelId { get; private init; }

    public string Number { get; private init; } = string.Empty;

    public string Outcome { get; private init; } = string.Empty;

    public string? CustomerCode { get; private init; }

    public string? CustomerName { get; private init; }

    public bool Queued { get; private init; }

    public static ScanOperationsParcelResponse Success(Guid parcelId, string number, string outcome, string? customerCode, string? customerName, bool queued)
    {
        return new ScanOperationsParcelResponse { Succeeded = true, ParcelId = parcelId, Number = number, Outcome = outcome, CustomerCode = customerCode, CustomerName = customerName, Queued = queued };
    }

    public static ScanOperationsParcelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ScanOperationsParcelResponse { Error = error };
    }
}
