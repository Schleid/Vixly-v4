using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants;

// =========================================================
// FILE: PlatformTenantChanges.cs
//
// PURPOSE:
// What the company change request use cases share (B12).
//
// RESPONSIBILITIES:
// - Check staff permission and step-up
// - Record audit entries and outbox messages
// - Save
//
// IMPORTANT NOTES:
// - Tenant-side use cases check the tenant role with PlatformTenantAccess instead
// =========================================================
public sealed class PlatformTenantChanges
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformPermissionReader _permissions;

    private readonly PlatformAuditTrail _audit;

    private readonly PlatformOutboxWriter _outbox;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PlatformTenantChanges(
        IPlatformAuthorizationGate gate,
        IPlatformStepUpGuard stepUp,
        IPlatformPermissionReader permissions,
        IPlatformTenantRepository tenants,
        IPlatformTenantChangeRepository requests,
        PlatformAuditTrail audit,
        PlatformOutboxWriter outbox,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        Tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        Requests = requests ?? throw new ArgumentNullException(nameof(requests));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public IPlatformTenantRepository Tenants { get; }

    public IPlatformTenantChangeRepository Requests { get; }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public async Task<Error?> AuthorizeAsync(PlatformAuthorizationContext context, string permissionCode, bool requireStepUp, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        await _gate.AuthorizeAsync(context, permissionCode, cancellationToken);

        return requireStepUp && !await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken)
            ? PlatformCommonErrors.StepUpRequired
            : null;
    }

    public async Task<bool> CanReviewAsync(PlatformAuthorizationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        PlatformPermissionSnapshot? actor = await _permissions.ReadAsync(context.PlatformUserId, UtcNow, cancellationToken);
        return actor is not null && actor.IsAccountActive && PlatformAccess.Holds(actor, PlatformPermissions.TenantsReviewChanges);
    }

    public void Record(Guid? staffUserId, string actionCode, string? permissionCode, PlatformTenantChangeRequest request, object? arguments = null, object? before = null, object? after = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        _audit.Record(staffUserId, actionCode, permissionCode, nameof(PlatformTenantChangeRequest), request.Id, arguments: arguments, before: before, after: after);
    }

    public void Notify(string messageType, PlatformTenantChangeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _outbox.Write(messageType, request.PlatformTenantId, new PlatformTenantCompanyChangeMessage(request.Id, request.PlatformTenantId, UtcNow));
    }

    public Task<bool> SaveAsync(CancellationToken cancellationToken) => _unitOfWork.SaveChangesAsync(cancellationToken);

    public static object Describe(PlatformCompanyDetails company)
    {
        ArgumentNullException.ThrowIfNull(company);
        return new
        {
            legalName = company.LegalName,
            tradingName = company.TradingName,
            email = company.Email.Value,
            phone = company.Phone.Value,
            line1 = company.Address.Line1,
            line2 = company.Address.Line2,
            city = company.Address.City,
            region = company.Address.StateProvince,
            postalCode = company.Address.PostalCode,
            country = company.Address.CountryCode
        };
    }
}
