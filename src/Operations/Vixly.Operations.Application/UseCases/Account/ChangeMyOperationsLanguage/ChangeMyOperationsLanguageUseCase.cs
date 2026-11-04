using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Operations.Application.UseCases.Account.ChangeMyOperationsLanguage;

// =========================================================
// FILE: ChangeMyOperationsLanguageUseCase.cs
//
// PURPOSE:
// Saves the language of my pages and emails (Slice 1E).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ChangeMyOperationsLanguageUseCase
{
    private readonly IOperationsUserRepository _users;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeMyOperationsLanguageUseCase(
        IOperationsUserRepository users,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeMyOperationsLanguageResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeMyOperationsLanguageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsUser? user = context.UserId is { } userId ? await _users.GetAsync(userId, cancellationToken) : null;

        if (user is null)
        {
            return ChangeMyOperationsLanguageResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (!LanguageCode.TryCreate(request.Language, out LanguageCode? language) || language!.Value is not ("en" or "fr" or "es"))
        {
            return ChangeMyOperationsLanguageResponse.Failure(OperationsCommonErrors.RequestInvalid);
        }

        user.ChangePreferredLanguage(language, _timeProvider.GetUtcNow());
        _audit.Record(user.Id, "ops.account.language_changed", nameof(OperationsUser), user.Id, after: new { language = language.Value });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeMyOperationsLanguageResponse.Success()
            : ChangeMyOperationsLanguageResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
