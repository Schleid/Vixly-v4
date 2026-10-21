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
//
// IMPORTANT NOTES:
// - The caller saves (the code cannot be used twice); passkeys and the 5-minute window arrive with 1D
// =========================================================
public sealed class OperationsStepUp
{
    private readonly IOperationsAuthenticatorCodes _codes;

    private readonly TimeProvider _timeProvider;

    public OperationsStepUp(IOperationsAuthenticatorCodes codes, TimeProvider timeProvider)
    {
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Error? Check(OperationsUser user, string? code)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(code))
        {
            return OperationsDeviceErrors.CodeRequired;
        }

        if (user.AuthenticatorApp is not { IsConfirmed: true } app)
        {
            return OperationsAuthErrors.CodeInvalid;
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
