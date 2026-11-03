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

    private readonly IOperationsIdleTimesReader _idle;

    public UpdateOperationsTenantSettingsUseCase(
        IOperationsChargeRepository charges,
        IOperationsBillingRepository billing,
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IOperationsIdleTimesReader idle)
    {
        _idle = idle ?? throw new ArgumentNullException(nameof(idle));
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

        // Require MFA (owner, 2026-10-22): only the Owner and Administrators change it, always with a fresh confirmation.
        bool mfaChanges = request.RequireMfa is bool wanted && wanted != (saved?.RequireMfa ?? true);

        // The inactivity times are security settings too (owner, 2026-10-27): the same people, with a fresh confirmation.
        int staffIdle = request.StaffIdleMinutes ?? saved?.StaffIdleMinutes ?? OperationsSettingsLimits.StaffIdleMinutesDefault;
        int customerIdle = request.CustomerIdleMinutes ?? saved?.CustomerIdleMinutes ?? OperationsSettingsLimits.CustomerIdleMinutesDefault;
        int idleWarn = request.IdleWarnMinutes ?? saved?.IdleWarnMinutes ?? OperationsSettingsLimits.IdleWarnMinutesDefault;
        bool idleChanges = staffIdle != (saved?.StaffIdleMinutes ?? OperationsSettingsLimits.StaffIdleMinutesDefault)
            || customerIdle != (saved?.CustomerIdleMinutes ?? OperationsSettingsLimits.CustomerIdleMinutesDefault)
            || idleWarn != (saved?.IdleWarnMinutes ?? OperationsSettingsLimits.IdleWarnMinutesDefault);
        bool currencyChanges = !string.Equals((saved?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency), request.CurrencyCode?.Trim(), StringComparison.OrdinalIgnoreCase);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, weakens || currencyChanges || mfaChanges || idleChanges, cancellationToken);

        if (refused is not null)
        {
            return UpdateOperationsTenantSettingsResponse.Failure(refused);
        }

        if ((mfaChanges || idleChanges) && !access!.User.RequiresSecondFactor)
        {
            return UpdateOperationsTenantSettingsResponse.Failure(OperationsAccessErrors.PermissionRequired);
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
            settings.ChangePublicPricing(request.PublicPricing, utcNow);
            settings.ChangePrefixes(request.ShipmentPrefix, request.ReceivingPrefix, request.InvoicePrefix, request.PaymentPrefix, request.CustomerPrefix ?? settings.CustomerPrefix, request.ParcelPrefix ?? settings.ParcelPrefix,
                request.SupplyOrderPrefix ?? settings.SupplyOrderPrefix, request.CreditNotePrefix ?? settings.CreditNotePrefix, request.BundlePrefix ?? settings.BundlePrefix,
                request.BoxPrefix ?? settings.BoxPrefix, request.DispatchPrefix ?? settings.DispatchPrefix, utcNow);
            settings.RequireSecondFactor(request.RequireSecondFactorForAllStaff, utcNow);

            if (request.TrackingPrefix is not null)
            {
                settings.ChangeTrackingPrefix(request.TrackingPrefix, utcNow);
            }

            if (mfaChanges)
            {
                settings.ChangeRequireMfa(request.RequireMfa!.Value, utcNow);
            }

            if (idleChanges)
            {
                settings.ChangeIdle(staffIdle, customerIdle, idleWarn, utcNow);
            }

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

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return UpdateOperationsTenantSettingsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        // New inactivity times apply from each person's next page on this server; other servers within the minute.
        _idle.Forget();
        return UpdateOperationsTenantSettingsResponse.Success();
    }

    private static object Snapshot(OperationsTenantSettings settings) => new
    {
        currency = settings.CurrencyCode,
        weight = settings.WeightUnit.ToString(),
        dimension = settings.DimensionUnit.ToString(),
        collect = settings.AllowCollect,
        credit = settings.AllowCredit,
        publicPricing = settings.PublicPricing,
        delivery = settings.DeliveryModes.ToString(),
        secondFactorForAll = settings.RequireSecondFactorForAllStaff,
        requireMfa = settings.RequireMfa,
        idle = new[] { settings.StaffIdleMinutes, settings.CustomerIdleMinutes, settings.IdleWarnMinutes },
        prefixes = new[] { settings.ShipmentPrefix, settings.ReceivingPrefix, settings.InvoicePrefix, settings.PaymentPrefix, settings.CustomerPrefix, settings.ParcelPrefix, settings.SupplyOrderPrefix, settings.CreditNotePrefix,
            settings.BundlePrefix, settings.BoxPrefix, settings.DispatchPrefix },
        tracking = settings.TrackingPrefix
    };
}
