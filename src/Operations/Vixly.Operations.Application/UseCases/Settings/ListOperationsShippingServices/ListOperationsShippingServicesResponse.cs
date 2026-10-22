using Vixly.Operations.Application.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Settings.ListOperationsShippingServices;

// =========================================================
// FILE: ListOperationsShippingServicesResponse.cs
//
// PURPOSE:
// Lists Vixly's shipping services with the tenant's setup of each (Slice 1E, D-O15).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsShippingServicesResponse
{
    private ListOperationsShippingServicesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsShippingServiceRow> Services { get; private init; } = Array.Empty<OperationsShippingServiceRow>();

    public string DivisorUnit { get; private init; } = string.Empty;

    public bool CanEdit { get; private init; }

    public static ListOperationsShippingServicesResponse Success(IReadOnlyList<OperationsShippingServiceRow> services, string divisorUnit, bool canEdit)
    {
        return new ListOperationsShippingServicesResponse { Succeeded = true, Services = services, DivisorUnit = divisorUnit, CanEdit = canEdit };
    }

    public static ListOperationsShippingServicesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsShippingServicesResponse { Error = error };
    }
}
