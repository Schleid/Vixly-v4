using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Messaging;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Setup;

// =========================================================
// FILE: OperationsOwnerInvitationHandler.cs
//
// PURPOSE:
// Invites the owner of a tenant that is ready for setup (Slice 1, D-O3).
//
// RESPONSIBILITIES:
// - Read the owner contact and company names from Platform (GET /tenant)
// - Create the Owner (Invited), the brand name and a 72-hour invitation link
// - Email the link to the tenant host in the tenant language
//
// IMPORTANT NOTES:
// - Runs in the tenant scope of the message
// - Does nothing when the tenant already has staff, so a replayed event never invites twice
// - The email goes before the commit: if the commit fails, the retry sends a new link and the first one never works
// =========================================================
public sealed class OperationsOwnerInvitationHandler : IOperationsOutboxHandler
{
    private readonly IOperationsPlatformClient _platform;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsAccountTokenRepository _tokens;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly IOperationsIdentityMailer _mailer;

    private readonly IOperationsPortalLinks _links;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OperationsOwnerInvitationHandler(
        IOperationsPlatformClient platform,
        IOperationsUserRepository users,
        IOperationsAccountTokenRepository tokens,
        IOperationsBrandRepository brands,
        IOperationsSecretGenerator secrets,
        IOperationsIdentityMailer mailer,
        IOperationsPortalLinks links,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public string MessageType => OperationsSetupMessages.OwnerInvitationRequested;

    public async Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        Guid tenantId = message.TenantId ?? throw new DomainException(OperationsCommonErrors.TenantRequired);

        if (await _users.AnyAsync(cancellationToken))
        {
            return;
        }

        OperationsTenantAccount? account = await _platform.GetTenantAsync(tenantId, cancellationToken);

        if (account is null || !EmailAddress.TryCreate(account.OwnerEmail, out EmailAddress? email))
        {
            throw new DomainException(OperationsSetupErrors.OwnerContactMissing);
        }

        LanguageCode language = LanguageCode.TryCreate(account.DefaultLanguage, out LanguageCode? parsed) ? parsed! : LanguageCode.Default;
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsUser owner = OperationsUser.InviteOwner(tenantId, string.IsNullOrWhiteSpace(account.OwnerName) ? email!.Value : account.OwnerName, email!, language, utcNow);
        OperationsTenantBrand? brand = await _brands.FindAsync(cancellationToken);

        if (brand is null)
        {
            brand = OperationsTenantBrand.Create(tenantId, account.DisplayName, utcNow);
            _brands.Add(brand);
        }

        OperationsSecret secret = _secrets.NewToken();
        _users.Add(owner);
        _tokens.Add(OperationsAccountToken.Issue(tenantId, owner.Id, OperationsAccountTokenPurpose.Invitation, secret.Hash, utcNow));
        _audit.Record(null, "ops.setup.owner_invited", nameof(OperationsUser), owner.Id);

        await _mailer.SendOwnerInvitationAsync(
            new OperationsOwnerInvitationMail(
                email!.Value,
                owner.FullName,
                language,
                brand.DisplayName,
                _links.Page(account.Slug, "/invitation?token=" + Uri.EscapeDataString(secret.Value)),
                (int)OperationsIdentityLimits.InvitationLifetime.TotalHours),
            cancellationToken);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            throw new DomainException(OperationsCommonErrors.ConcurrencyConflict);
        }
    }
}
