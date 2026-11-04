using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Pickup.SendOperationsPickupCode;

// =========================================================
// FILE: SendOperationsPickupCodeUseCase.cs
//
// PURPOSE:
// Send the customer a new pickup code (Slice 7 step 2, R-5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.pickup.release at the warehouse, when the company asks for a code
// - A new code replaces the last one
// =========================================================
public sealed class SendOperationsPickupCodeUseCase
{
    private readonly OperationsPickupDesk _desk;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly OperationsPickupCodes _codes;

    public SendOperationsPickupCodeUseCase(
        OperationsPickupDesk desk,
        IOperationsCustomerRepository customers,
        OperationsCustomerAccounts accounts,
        IOperationsSettingsRepository settings,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        OperationsPickupCodes codes)
    {
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SendOperationsPickupCodeResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SendOperationsPickupCodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return SendOperationsPickupCodeResponse.Failure(refused);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) is not { } here
            || await _desk.ShipmentAsync(request.ShipmentId, here, cancellationToken) is not { } view)
        {
            return SendOperationsPickupCodeResponse.Failure(OperationsPickupErrors.NotFound);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());

        if (!settings.PickupProofs.HasFlag(OperationsPickupProofs.Code) || view.Ready.Count == 0 || string.IsNullOrWhiteSpace(request.PortalSlug)
            || await _customers.GetAsync(view.Shipment.CustomerId, cancellationToken) is not { Status: Vixly.Operations.Domain.Customers.OperationsCustomerStatus.Active } customer)
        {
            return SendOperationsPickupCodeResponse.Failure(OperationsPickupErrors.CodeNotSent);
        }

        // At most one new code every two minutes (review); kept protected, emailed after the save, never written to the outbox.
        if (view.Shipment.PickupCodeUtc is { } last && last + OperationsPickupCode.ResendAfter > _timeProvider.GetUtcNow())
        {
            return SendOperationsPickupCodeResponse.Failure(OperationsPickupErrors.CodeWait);
        }

        string code = _codes.Issue(view.Shipment);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return SendOperationsPickupCodeResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        await OperationsPickupCodeMail.SendAsync(_accounts, customer, request.PortalSlug, view.Shipment.Number ?? string.Empty, here.Name, code, cancellationToken);
        return SendOperationsPickupCodeResponse.Success();
    }
}
