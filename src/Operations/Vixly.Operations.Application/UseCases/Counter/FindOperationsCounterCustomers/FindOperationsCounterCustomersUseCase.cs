using Vixly.Operations.Application.Counter;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.FindOperationsCounterCustomers;

// =========================================================
// FILE: FindOperationsCounterCustomersUseCase.cs
//
// PURPOSE:
// Finds a customer at the front-desk counter by phone, email or customer code (Slice 5C, C-5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.counter.serve somewhere; a name alone finds nobody (the owner: phone or email)
// - At most ten matches
// =========================================================
public sealed class FindOperationsCounterCustomersUseCase
{
    private readonly OperationsCounterDesk _desk;

    private readonly Vixly.Operations.Application.Customers.OperationsCustomerLookups _lookups;

    public FindOperationsCounterCustomersUseCase(
        OperationsCounterDesk desk,
        Vixly.Operations.Application.Customers.OperationsCustomerLookups lookups)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _lookups = lookups ?? throw new ArgumentNullException(nameof(lookups));
    }

    public async Task<FindOperationsCounterCustomersResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        FindOperationsCounterCustomersRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> _, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return FindOperationsCounterCustomersResponse.Failure(refused);
        }

        string text = request.Query?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            return FindOperationsCounterCustomersResponse.Success(Array.Empty<OperationsCounterCustomer>());
        }

        // Owner, 2026-10-29: recorded, and limited per hour (design 4.4).
        (IReadOnlyList<OperationsCustomer> found, Error? limit) = await _lookups.FindAsync(access!, text, 10, cancellationToken);
        return limit is not null
            ? FindOperationsCounterCustomersResponse.Failure(limit)
            : FindOperationsCounterCustomersResponse.Success(found.Select(OperationsCounterCustomer.Of).ToList());
    }
}
