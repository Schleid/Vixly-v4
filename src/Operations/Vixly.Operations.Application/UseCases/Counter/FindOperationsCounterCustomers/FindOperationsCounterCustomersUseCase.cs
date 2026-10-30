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

    private readonly IOperationsCustomerRepository _customers;

    public FindOperationsCounterCustomersUseCase(
        OperationsCounterDesk desk,
        IOperationsCustomerRepository customers)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
    }

    public async Task<FindOperationsCounterCustomersResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        FindOperationsCounterCustomersRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, IReadOnlyList<OperationsWarehouse> _, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return FindOperationsCounterCustomersResponse.Failure(refused);
        }

        string text = request.Query?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            return FindOperationsCounterCustomersResponse.Success(Array.Empty<OperationsCounterCustomer>());
        }

        IReadOnlyList<OperationsCustomer> found = await _customers.FindForCounterAsync(text, 10, cancellationToken);
        return FindOperationsCounterCustomersResponse.Success(found.Select(OperationsCounterCustomer.Of).ToList());
    }
}
