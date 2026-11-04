using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Screens;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Screens;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Screens.ListOperationsCustomerScreens;

// =========================================================
// FILE: ListOperationsCustomerScreensUseCase.cs
//
// PURPOSE:
// The customer screens of the warehouses where the person serves or takes payments (Slice 5F step 3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.counter.serve or ops.payments.take at the screen’s warehouse
// =========================================================
public sealed class ListOperationsCustomerScreensUseCase
{
    private readonly OperationsScreenDesk _desk;

    public ListOperationsCustomerScreensUseCase(
        OperationsScreenDesk desk)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
    }

    public async Task<ListOperationsCustomerScreensResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsCustomerScreensRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        return refused is not null
            ? ListOperationsCustomerScreensResponse.Failure(refused)
            : ListOperationsCustomerScreensResponse.Success(await _desk.RowsAsync(warehouses, cancellationToken), warehouses);
    }
}
