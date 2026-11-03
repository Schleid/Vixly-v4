using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Payables;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Payables;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Payables.ListOperationsVendors;

// =========================================================
// FILE: ListOperationsVendorsResponse.cs
//
// PURPOSE:
// The answer to listOperationsVendors.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsVendorsResponse
{
    private ListOperationsVendorsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsVendor> Vendors { get; private init; } = Array.Empty<OperationsVendor>();

    public static ListOperationsVendorsResponse Success(IReadOnlyList<OperationsVendor> vendors)
    {
        return new ListOperationsVendorsResponse { Succeeded = true, Vendors = vendors };
    }

    public static ListOperationsVendorsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsVendorsResponse { Error = error };
    }
}
