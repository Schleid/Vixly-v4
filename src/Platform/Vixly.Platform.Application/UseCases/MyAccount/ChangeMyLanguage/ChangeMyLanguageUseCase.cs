using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases.MyAccount.ChangeMyLanguage;

// =========================================================
// FILE: ChangeMyLanguageUseCase.cs
//
// PURPOSE:
// The signed-in user chooses the language of the console and of
// their emails.
//
// RESPONSIBILITIES:
// - Check the language, save it, audit, commit
//
// IMPORTANT NOTES:
// - The console uses it from the next page load (ADR-0005)
// =========================================================
public sealed class ChangeMyLanguageUseCase
{
    private readonly IPlatformUserRepository _users;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeMyLanguageUseCase(
        IPlatformUserRepository users,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeMyLanguageResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ChangeMyLanguageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformUser? user = await _users.GetAsync(context.PlatformUserId, cancellationToken);

        if (user is null)
        {
            return ChangeMyLanguageResponse.Failure(PlatformCommonErrors.NotFound);
        }

        string before = user.PreferredLanguage.Value;

        try
        {
            user.ChangePreferredLanguage(LanguageCode.Create(request.Language), _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return ChangeMyLanguageResponse.Failure(exception.Error);
        }

        _audit.Record(user.Id, "platform.account.language_changed", null, nameof(PlatformUser), user.Id, before: new { language = before }, after: new { language = user.PreferredLanguage.Value });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeMyLanguageResponse.Success()
            : ChangeMyLanguageResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
