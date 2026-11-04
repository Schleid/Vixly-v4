using System.Collections.Concurrent;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Settings.AdoptOperationsCompanyTimeZone;

// =========================================================
// FILE: AdoptOperationsCompanyTimeZoneUseCase.cs
//
// PURPOSE:
// Gives Operations the company's time zone the first time a tenant is used (owner, 2026-11-02).
//
// RESPONSIBILITIES:
// - Copy the company's IANA time zone from Platform into the Operations settings while they have none
// - Leave a time zone someone already chose in Operations settings untouched
//
// IMPORTANT NOTES:
// - Runs from the tenant middleware: it never fails a request, and a tenant is checked once per process
// - UTC from Platform is not copied (an empty setting already means UTC)
// - A second request racing this one loses on the save and changes nothing
// =========================================================
public sealed class AdoptOperationsCompanyTimeZoneUseCase
{
    // Tenants already checked by this process; the setting only ever goes from empty to a zone here.
    private static readonly ConcurrentDictionary<Guid, bool> Checked = new();

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AdoptOperationsCompanyTimeZoneUseCase(
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public static bool AlreadyChecked(Guid tenantId) => Checked.ContainsKey(tenantId);

    public async Task<AdoptOperationsCompanyTimeZoneResponse> ExecuteAsync(
        OperationsAuthorizationContext authorizationContext,
        AdoptOperationsCompanyTimeZoneRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorizationContext);
        ArgumentNullException.ThrowIfNull(request);

        Guid tenantId = request.TenantId;

        if (tenantId == Guid.Empty || Checked.ContainsKey(tenantId))
        {
            return AdoptOperationsCompanyTimeZoneResponse.Success(false);
        }

        string? zone = string.IsNullOrWhiteSpace(request.CompanyTimeZone) ? null : request.CompanyTimeZone.Trim();

        if (zone is null || string.Equals(zone, "UTC", StringComparison.OrdinalIgnoreCase))
        {
            Checked[tenantId] = true;
            return AdoptOperationsCompanyTimeZoneResponse.Success(false);
        }

        OperationsTenantSettings? saved = await _settings.FindAsync(cancellationToken);

        if (saved?.TimeZone is not null)
        {
            Checked[tenantId] = true;
            return AdoptOperationsCompanyTimeZoneResponse.Success(false);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings settings = saved ?? OperationsTenantSettings.CreateDefault(tenantId, utcNow);

        try
        {
            settings.ChangeTimeZone(zone, utcNow);
        }
        catch (DomainException)
        {
            // Platform sent a zone this host does not know: keep UTC rather than guess.
            Checked[tenantId] = true;
            return AdoptOperationsCompanyTimeZoneResponse.Success(false);
        }

        if (saved is null)
        {
            _settings.Add(settings);
        }

        _audit.Record(null, "ops.settings.time_zone_from_company", nameof(OperationsTenantSettings), settings.Id, after: new { timeZone = zone });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            Checked[tenantId] = true;
            return AdoptOperationsCompanyTimeZoneResponse.Success(true);
        }

        // A request racing this one saved first; the next request checks again.
        _unitOfWork.DiscardChanges();
        return AdoptOperationsCompanyTimeZoneResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
