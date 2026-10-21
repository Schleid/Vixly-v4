using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Settings.UpdateOperationsTenantSettings;

// =========================================================
// FILE: UpdateOperationsTenantSettingsUseCase.cs
//
// PURPOSE:
// Saves the tenant's settings (Slice 1E).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class UpdateOperationsTenantSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly IOperationsChargeRepository _charges;

    private readonly IOperationsBillingRepository _billing;

    public UpdateOperationsTenantSettingsUseCase(
        IOperationsChargeRepository charges,
        IOperationsBillingRepository billing,
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
    }

    public async Task<UpdateOperationsTenantSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        UpdateOperationsTenantSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsTenantSettings? saved = await _settings.FindAsync(cancellationToken);

        // Turning off the staff second-factor rule weakens security: it needs a fresh code (step-up).
        // Changing the billing currency is sensitive too (design section 3).
        bool weakens = saved?.RequireSecondFactorForAllStaff == true && !request.RequireSecondFactorForAllStaff;
        bool currencyChanges = !string.Equals((saved?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency), request.CurrencyCode?.Trim(), StringComparison.OrdinalIgnoreCase);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, weakens || currencyChanges, cancellationToken);

        if (refused is not null)
        {
            return UpdateOperationsTenantSettingsResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings settings = saved ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);

        // Slice 4B: prices are typed per lb or kg, per inch or cm; once a price list exists the units stay.
        if ((request.WeightUnit != settings.WeightUnit || request.DimensionUnit != settings.DimensionUnit) && await _charges.AnyCardAsync(cancellationToken))
        {
            return UpdateOperationsTenantSettingsResponse.Failure(Vixly.Operations.Domain.Charges.OperationsChargeErrors.UnitLocked);
        }

        object before = Snapshot(settings);
        bool anyInvoice = await _billing.AnyInvoiceAsync(cancellationToken);

        try
        {
            // Slice 5F: the currency is fixed once an invoice exists.
            settings.ChangeCurrency(request.CurrencyCode, anyInvoice, utcNow);
            settings.ChangeWork(request.WeightUnit, request.DimensionUnit, request.AllowCollect, request.AllowCredit, request.DeliveryModes, utcNow);
            settings.ChangePrefixes(request.ShipmentPrefix, request.ReceivingPrefix, request.InvoicePrefix, request.PaymentPrefix, request.CustomerPrefix ?? settings.CustomerPrefix, request.ParcelPrefix ?? settings.ParcelPrefix,
                request.SupplyOrderPrefix ?? settings.SupplyOrderPrefix, request.CreditNotePrefix ?? settings.CreditNotePrefix, utcNow);
            settings.RequireSecondFactor(request.RequireSecondFactorForAllStaff, utcNow);

            if (request.TimeZone is not null)
            {
                settings.ChangeTimeZone(request.TimeZone, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return UpdateOperationsTenantSettingsResponse.Failure(exception.Error);
        }

        if (saved is null)
        {
            _settings.Add(settings);
        }

        _audit.Record(access!.User.Id, "ops.settings.changed", nameof(OperationsTenantSettings), settings.Id, before: before, after: Snapshot(settings));

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? UpdateOperationsTenantSettingsResponse.Success()
            : UpdateOperationsTenantSettingsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    private static object Snapshot(OperationsTenantSettings settings) => new
    {
        currency = settings.CurrencyCode,
        weight = settings.WeightUnit.ToString(),
        dimension = settings.DimensionUnit.ToString(),
        collect = settings.AllowCollect,
        credit = settings.AllowCredit,
        delivery = settings.DeliveryModes.ToString(),
        secondFactorForAll = settings.RequireSecondFactorForAllStaff,
        prefixes = new[] { settings.ShipmentPrefix, settings.ReceivingPrefix, settings.InvoicePrefix, settings.PaymentPrefix, settings.CustomerPrefix, settings.ParcelPrefix, settings.SupplyOrderPrefix, settings.CreditNotePrefix }
    };
}
