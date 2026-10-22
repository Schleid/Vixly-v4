using Vixly.Operations.Application.Cards;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsCardSettings;

// =========================================================
// FILE: GetOperationsCardSettingsResponse.cs
//
// PURPOSE:
// The company's Stripe account and card readers as Settings → Payments shows them (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCardSettingsResponse
{
    private GetOperationsCardSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool Connected { get; private init; }

    public string? KeyHint { get; private init; }

    public bool Live { get; private init; }

    public string? AccountName { get; private init; }

    public bool AcceptCard { get; private init; }

    public bool CanEdit { get; private init; }

    public IReadOnlyList<OperationsCardReaderRow> Readers { get; private init; } = Array.Empty<OperationsCardReaderRow>();

    // The active warehouses a reader may be placed at.
    public IReadOnlyList<Vixly.Operations.Application.Shipments.OperationsChoice> Warehouses { get; private init; } = Array.Empty<Vixly.Operations.Application.Shipments.OperationsChoice>();

    public static GetOperationsCardSettingsResponse Success(bool connected, string? keyHint, bool live, string? accountName, bool acceptCard, bool canEdit, IReadOnlyList<OperationsCardReaderRow> readers, IReadOnlyList<Vixly.Operations.Application.Shipments.OperationsChoice> warehouses)
    {
        return new GetOperationsCardSettingsResponse { Succeeded = true, Connected = connected, KeyHint = keyHint, Live = live, AccountName = accountName, AcceptCard = acceptCard, CanEdit = canEdit, Readers = readers, Warehouses = warehouses };
    }

    public static GetOperationsCardSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCardSettingsResponse { Error = error };
    }
}
