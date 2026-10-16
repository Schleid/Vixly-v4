using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases.TenantAccount.UpdateTenantAccount;

// =========================================================
// FILE: UpdateTenantAccountUseCase.cs
//
// PURPOSE:
// The tenant changes its billing and technical contacts, language and time zone (call 16).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel for the tenant in the service token only (S-13, D-K8)
// - Owner or admin with a second factor checked
// - Null contacts keeps them; a list replaces the billing and technical contacts; the owner contact stays (Vixly staff change it)
// =========================================================
public sealed class UpdateTenantAccountUseCase
{
    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly PlatformAuditTrail _audit;

    private readonly TimeProvider _timeProvider;

    public UpdateTenantAccountUseCase(
        IPlatformTenantRepository tenants,
        IPlatformUnitOfWork unitOfWork,
        PlatformAuditTrail audit,
        TimeProvider timeProvider)
    {
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<UpdateTenantAccountResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        UpdateTenantAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? notAllowed = PlatformTenantAccess.Require(context, true, PlatformTenantActorRole.Owner, PlatformTenantActorRole.Admin);

        if (notAllowed is not null)
        {
            return UpdateTenantAccountResponse.Failure(notAllowed);
        }

        PlatformTenant? tenant = await _tenants.GetAsync(context.PlatformTenantId, cancellationToken);

        if (tenant is null)
        {
            return UpdateTenantAccountResponse.Failure(PlatformCommonErrors.NotFound);
        }

        if (request.Contacts?.Any(contact => contact.Role == PlatformTenantContactRole.Owner) == true)
        {
            return UpdateTenantAccountResponse.Failure(PlatformTenantAccountErrors.OwnerContactLocked);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        object contactsBefore = Describe(tenant);
        object settingsBefore = new { language = tenant.DefaultLanguage.Value, timeZone = tenant.TimeZone };
        object actor = new { actor = context.ActorReference, actorName = context.ActorName, role = PlatformTenantAccess.RoleName(context.Role) };
        bool settings = request.Language is not null || request.TimeZone is not null;

        try
        {
            if (request.Contacts is { } contacts)
            {
                List<PlatformTenantContactDetails> replaced = tenant.Contacts
                    .Where(contact => contact.Role == PlatformTenantContactRole.Owner)
                    .Select(contact => PlatformTenantContactDetails.Create(contact.Role, contact.Name, contact.Email, contact.Phone))
                    .ToList();
                replaced.AddRange(contacts.Select(contact => contact.ToDetails()));
                tenant.ReplaceContacts(replaced, utcNow);
            }

            if (settings)
            {
                tenant.UpdateSettings(request.Language is null ? tenant.DefaultLanguage : LanguageCode.Create(request.Language), request.TimeZone ?? tenant.TimeZone, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return UpdateTenantAccountResponse.Failure(exception.Error);
        }

        if (request.Contacts is not null)
        {
            _audit.Record(null, "platform.tenants.contacts_updated", null, nameof(PlatformTenant), tenant.Id, arguments: actor, before: contactsBefore, after: Describe(tenant));
        }

        if (settings)
        {
            _audit.Record(null, "platform.tenants.settings_updated", null, nameof(PlatformTenant), tenant.Id, arguments: actor, before: settingsBefore,
                after: new { language = tenant.DefaultLanguage.Value, timeZone = tenant.TimeZone });
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? UpdateTenantAccountResponse.Success()
            : UpdateTenantAccountResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }

    private static object Describe(PlatformTenant tenant)
    {
        return tenant.Contacts
            .OrderBy(contact => contact.Role)
            .Select(contact => new { role = contact.Role.ToString(), contact.Name, email = contact.Email.Value })
            .ToArray();
    }
}
