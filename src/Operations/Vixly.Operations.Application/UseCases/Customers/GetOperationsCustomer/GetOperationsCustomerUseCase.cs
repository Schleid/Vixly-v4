using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.Customers.GetOperationsCustomer;

// =========================================================
// FILE: GetOperationsCustomerUseCase.cs
//
// PURPOSE:
// Staff open one customer, with the packages coming to them (Slice 2; owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// - No mailbox street address for staff; packages only at the warehouses the person may see parcels of
// =========================================================
public sealed class GetOperationsCustomerUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsReceivingScope _scope;

    public GetOperationsCustomerUseCase(
        OperationsAccess access,
        IOperationsCustomerRepository customers,
        IOperationsReceivingRepository receiving,
        OperationsReceivingScope scope)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public async Task<GetOperationsCustomerResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCustomerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CustomersView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCustomerResponse.Failure(refused);
        }

        if (await _customers.GetAsync(request.CustomerId, cancellationToken) is not { } customer)
        {
            return GetOperationsCustomerResponse.Failure(OperationsCommonErrors.NotFound);
        }

        // What is coming to the customer: announced packages, and parcels still at an origin warehouse the person may see.
        IReadOnlyList<OperationsWarehouse> covered = await _scope.WarehousesAsync(access!, OperationsPermissions.ParcelsView, false, cancellationToken);
        List<OperationsIncomingPackage> incoming = new();

        if (covered.Count > 0)
        {
            Dictionary<Guid, string> codes = covered.ToDictionary(warehouse => warehouse.Id, warehouse => warehouse.Code);
            incoming.AddRange((await _receiving.ListOpenPreAlertsAsync(customer.Id, cancellationToken))
                .Select(preAlert => new OperationsIncomingPackage(null, null, preAlert.Carrier, preAlert.TrackingNumber, preAlert.Contents, null, false, preAlert.CreatedUtc)));
            incoming.AddRange((await _receiving.ListCustomerParcelsAsync(customer.Id, cancellationToken))
                .Where(parcel => parcel.IsAtOrigin && parcel.WarehouseId is { } warehouse && codes.ContainsKey(warehouse))
                .Select(parcel => new OperationsIncomingPackage(parcel.Id, parcel.Number, parcel.Carrier, parcel.CarrierTrackingNumber ?? string.Empty, parcel.DeclaredContents,
                    codes[parcel.WarehouseId!.Value], parcel.Flags.HasFlag(OperationsParcelFlags.OnHold), parcel.CreatedUtc)));
        }

        return GetOperationsCustomerResponse.Success(OperationsCustomerProfile.Of(customer, Array.Empty<OperationsMailboxAddress>()),
            incoming.OrderByDescending(item => item.Since).ToList(), covered.Count > 0,
            access!.HasTenantWide(OperationsPermissions.CustomersManage), access.HasTenantWide(OperationsPermissions.CustomersCredit));
    }
}
