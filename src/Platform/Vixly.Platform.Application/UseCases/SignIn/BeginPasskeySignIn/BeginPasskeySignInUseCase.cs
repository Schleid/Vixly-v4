using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.SignIn.BeginPasskeySignIn;

// =========================================================
// FILE: BeginPasskeySignInUseCase.cs
//
// PURPOSE:
// Starts "Sign in with a passkey" (ADR-0006 section 2).
//
// RESPONSIBILITIES:
// - Build options that accept any passkey for this site
// - Store them in a 5-minute, single-use ceremony
//
// IMPORTANT NOTES:
// - Anonymous; rate-limited by the endpoint (S-6)
// - Reveals nothing about which accounts exist
// =========================================================
public sealed class BeginPasskeySignInUseCase
{
    private readonly IPlatformPasskeys _passkeys;

    private readonly PlatformPasskeyCeremonies _ceremonies;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public BeginPasskeySignInUseCase(
        IPlatformPasskeys passkeys,
        PlatformPasskeyCeremonies ceremonies,
        IPlatformUnitOfWork unitOfWork)
    {
        _passkeys = passkeys ?? throw new ArgumentNullException(nameof(passkeys));
        _ceremonies = ceremonies ?? throw new ArgumentNullException(nameof(ceremonies));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<BeginPasskeySignInResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        BeginPasskeySignInRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        string options = _passkeys.CreateAssertionOptions(Array.Empty<byte[]>());
        string token = _ceremonies.Start(PlatformPasskeyCeremonyPurpose.SignIn, null, options);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? BeginPasskeySignInResponse.Success(token, options)
            : BeginPasskeySignInResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
