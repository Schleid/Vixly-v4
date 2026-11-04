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
// FILE: ListOperationsVendorsUseCase.cs
//
// PURPOSE:
// The company’s other vendors (Slice 8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bills.enter
// =========================================================
public sealed class ListOperationsVendorsUseCase
{
    private readonly OperationsPayablesDesk _desk;

    public ListOperationsVendorsUseCase(
        OperationsPayablesDesk desk)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
    }

    public async Task<ListOperationsVendorsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsVendorsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, Error? refused) = await _desk.RequireAsync(context, OperationsPermissions.BillsEnter, false, cancellationToken);

        return refused is not null
            ? ListOperationsVendorsResponse.Failure(refused)
            : ListOperationsVendorsResponse.Success(await _desk.Payables.ListVendorsAsync(cancellationToken));
    }
}
