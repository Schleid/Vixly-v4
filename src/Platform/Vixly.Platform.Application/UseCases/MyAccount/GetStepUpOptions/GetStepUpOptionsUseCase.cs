using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.MyAccount.GetStepUpOptions;

// =========================================================
// FILE: GetStepUpOptionsUseCase.cs
//
// PURPOSE:
// Tells the step-up dialog whether the person confirms with the
// password (Require MFA is No and they have no second factor)
// or with their second factor (owner, 2026-10-22).
//
// RESPONSIBILITIES:
// - Read the session's person and the rule
//
// IMPORTANT NOTES:
// - Only a hint for the dialog: ConfirmSecondFactor checks the
//   same rule again
// =========================================================
public sealed class GetStepUpOptionsUseCase
{
    private readonly PlatformStepUpSteps _steps;

    public GetStepUpOptionsUseCase(PlatformStepUpSteps steps)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }

    public async Task<GetStepUpOptionsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetStepUpOptionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _steps.LoadAsync(context, cancellationToken) is not { } found)
        {
            return GetStepUpOptionsResponse.Failure(PlatformAuthErrors.SessionEnded);
        }

        return GetStepUpOptionsResponse.Success(await _steps.PasswordConfirmsAsync(found.User, cancellationToken));
    }
}
