using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Devices;

// =========================================================
// FILE: OperationsStepUp.cs
//
// PURPOSE:
// Checks a fresh authenticator code before a sensitive action (step-up, Slice 1C).
//
// RESPONSIBILITIES:
// - Verify the code of the signed-in user and spend its time step
// - Require MFA = No (owner, 2026-10-22): a person with no authenticator confirms with the password
//
// IMPORTANT NOTES:
// - The caller saves (the code cannot be used twice)
// - A person who set up an authenticator always uses it, whatever the company's rule
// =========================================================
public sealed class OperationsStepUp
{
    private readonly IOperationsAuthenticatorCodes _codes;

    private readonly TimeProvider _timeProvider;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsPasswordHasher _hasher;

    public OperationsStepUp(IOperationsAuthenticatorCodes codes, TimeProvider timeProvider, IOperationsSettingsRepository settings, IOperationsPasswordHasher hasher)
    {
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
    }

    // True when this person confirms with the password: no authenticator, and the company does not require MFA.
    public async Task<bool> PasswordConfirmsAsync(OperationsUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.AuthenticatorApp is not { IsConfirmed: true } && (await _settings.FindAsync(cancellationToken))?.RequireMfa == false;
    }

    public async Task<Error?> CheckAsync(OperationsUser user, string? code, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(code))
        {
            return OperationsDeviceErrors.CodeRequired;
        }

        if (user.AuthenticatorApp is not { IsConfirmed: true } app)
        {
            return await PasswordConfirmsAsync(user, cancellationToken)
                && user.Account.PasswordHash is { } hash
                && _hasher.Verify(code, hash) != OperationsPasswordCheck.Failed
                ? null
                : OperationsAuthErrors.CodeInvalid;
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        long? step = _codes.Verify(app.EncryptedSecret, code.Trim(), utcNow);

        if (step is null)
        {
            return OperationsAuthErrors.CodeInvalid;
        }

        try
        {
            user.RecordAuthenticatorSignIn(step.Value, utcNow);
            return null;
        }
        catch (DomainException)
        {
            return OperationsAuthErrors.CodeInvalid;
        }
    }
}
