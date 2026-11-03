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

namespace Vixly.Operations.Application.UseCases.Payables.GetOperationsBill;

// =========================================================
// FILE: GetOperationsBillUseCase.cs
//
// PURPOSE:
// One bill (Slice 8), or the empty form for a new one, with who can send bills and what the person may do with it.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bills.enter for a new one; any bill right to read one
// =========================================================
public sealed class GetOperationsBillUseCase
{
    private readonly OperationsPayablesDesk _desk;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsUserRepository _users;

    private readonly TimeProvider _timeProvider;

    public GetOperationsBillUseCase(
        OperationsPayablesDesk desk,
        IOperationsSettingsRepository settings,
        IOperationsLocationRepository locations,
        IOperationsUserRepository users,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsBillResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsBillRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = request.BillId is null
            ? await _desk.OpenAsync(context, cancellationToken, OperationsPermissions.BillsEnter)
            : await _desk.OpenAsync(context, cancellationToken, OperationsPermissions.BillsApprove, OperationsPermissions.BillsEnter, OperationsPermissions.CostsView);

        if (refused is not null)
        {
            return GetOperationsBillResponse.Failure(refused);
        }

        OperationsBill? bill = null;

        if (request.BillId is { } billId && (bill = await _desk.Payables.GetBillAsync(billId, cancellationToken)) is null)
        {
            return GetOperationsBillResponse.Failure(OperationsPayableErrors.NotFound);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        bool enter = access!.Has(OperationsPermissions.BillsEnter);
        bool approve = access.Has(OperationsPermissions.BillsApprove);
        List<OperationsBillPartyOption> parties = new();

        if (enter)
        {
            parties.AddRange((await _desk.Dispatches.ListCarriersAsync(cancellationToken)).Where(carrier => carrier.IsActive)
                .Select(carrier => new OperationsBillPartyOption(OperationsBillParty.Carrier, carrier.Id, carrier.Name, OperationsBillCategory.Freight)));
            parties.AddRange((await _locations.ListWarehousesAsync(cancellationToken)).Where(warehouse => warehouse.OperatedBy == OperationsDestinationOperator.Partner)
                .Select(warehouse => new OperationsBillPartyOption(OperationsBillParty.Partner, warehouse.Id, warehouse.PartnerName ?? warehouse.Name, OperationsBillCategory.PartnerFee)));
            parties.AddRange((await _desk.Payables.ListVendorsAsync(cancellationToken)).Where(vendor => vendor.Active)
                .Select(vendor => new OperationsBillPartyOption(OperationsBillParty.Vendor, vendor.Id, vendor.Name, vendor.Category)));
        }

        Dictionary<Guid, string> people = new();

        if (bill is not null)
        {
            foreach (Guid userId in new[] { bill.EnteredByUserId }.Concat(bill.DecidedByUserId is { } decided ? new[] { decided } : Array.Empty<Guid>())
                .Concat(bill.Payments.Select(payment => payment.RecordedByUserId)).Distinct())
            {
                if (await _users.GetAsync(userId, cancellationToken) is { } user)
                {
                    people[userId] = user.FullName;
                }
            }
        }

        // Review: the bill's own sender stays a choice when correcting it, even if no longer in use.
        if (bill is not null && enter && parties.All(option => option.Id != (bill.CarrierId ?? bill.PartnerWarehouseId ?? bill.VendorId)))
        {
            parties.Add(new OperationsBillPartyOption(bill.Party, (bill.CarrierId ?? bill.PartnerWarehouseId ?? bill.VendorId)!.Value, bill.PartyName, bill.Category));
        }

        bool own = bill is not null && (bill.EnteredByUserId == access.User.Id || bill.Editors.Contains(access.User.Id));
        return GetOperationsBillResponse.Success(bill, parties,
            enter && (bill is null || bill.Status is OperationsBillStatus.Entered or OperationsBillStatus.Rejected),
            approve && bill?.Status == OperationsBillStatus.Entered && (!own || !settings.BillsNeedApproval),
            approve && bill?.Status == OperationsBillStatus.Approved,
            approve && bill is { PaidTotal: 0m } && bill.Status is not (OperationsBillStatus.Voided or OperationsBillStatus.Paid),
            settings.BillsNeedApproval, settings.CurrencyCode, OperationsTimeZones.Today(_timeProvider.GetUtcNow(), settings.TimeZone), people);
    }
}
