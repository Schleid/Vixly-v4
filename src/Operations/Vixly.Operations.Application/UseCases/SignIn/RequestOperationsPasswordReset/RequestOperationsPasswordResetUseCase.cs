using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.SignIn.RequestOperationsPasswordReset;

// =========================================================
// FILE: RequestOperationsPasswordResetUseCase.cs
//
// PURPOSE:
// Emails a password reset link to a staff account (Slice 1C, "Forgot your password?").
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Same answer whether or not the address has an account
// - The link goes only to the account's own address, lasts 60 minutes and works once
// - The Owner resets the same way (owner, 2026-10-18); the second factor is still asked at the next sign-in
// =========================================================
public sealed class RequestOperationsPasswordResetUseCase
{
    private readonly IOperationsUserRepository _users;

    private readonly IOperationsAccountTokenRepository _tokens;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsIdentityMailer _mailer;

    private readonly IOperationsPortalLinks _links;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RequestOperationsPasswordResetUseCase(
        IOperationsUserRepository users,
        IOperationsAccountTokenRepository tokens,
        IOperationsSecretGenerator secrets,
        IOperationsBrandRepository brands,
        IOperationsIdentityMailer mailer,
        IOperationsPortalLinks links,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RequestOperationsPasswordResetResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RequestOperationsPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // The same answer whether or not the address has an account (no account discovery).
        if (!EmailAddress.TryCreate(request.Email, out EmailAddress? email) || string.IsNullOrWhiteSpace(request.PortalSlug))
        {
            return RequestOperationsPasswordResetResponse.Success();
        }

        OperationsUser? user = await _users.FindByEmailAsync(email!, cancellationToken);

        if (user is null || !user.Account.IsActive)
        {
            return RequestOperationsPasswordResetResponse.Success();
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsSecret secret = _secrets.NewToken();
        _tokens.Add(OperationsAccountToken.Issue(context.RequiredTenantId, user.Id, OperationsAccountTokenPurpose.PasswordReset, secret.Hash, utcNow));
        _audit.Record(user.Id, "ops.auth.password_reset_requested", nameof(OperationsUser), user.Id);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RequestOperationsPasswordResetResponse.Success();
        }

        OperationsTenantBrand? brand = await _brands.FindAsync(cancellationToken);
        await _mailer.SendPasswordResetAsync(
            new OperationsPasswordResetMail(
                user.Email.Value,
                user.FullName,
                user.PreferredLanguage,
                brand?.DisplayName ?? string.Empty,
                _links.Page(request.PortalSlug, "/staff/reset-password?token=" + Uri.EscapeDataString(secret.Value)),
                (int)OperationsIdentityLimits.PasswordResetLifetime.TotalMinutes),
            cancellationToken);

        return RequestOperationsPasswordResetResponse.Success();
    }
}
