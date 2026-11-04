using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsUserAuthenticatorApp.cs
//
// PURPOSE:
// A staff member authenticator app (TOTP) secret, encrypted (Slice 1).
//
// RESPONSIBILITIES:
// - Hold the encrypted secret and confirmation
// - Refuse a code time step already used
//
// IMPORTANT NOTES:
// - The secret is encrypted by Infrastructure; the domain never sees it in clear
// =========================================================
public sealed class OperationsUserAuthenticatorApp : OperationsEntity
{
    private OperationsUserAuthenticatorApp()
    {
        EncryptedSecret = string.Empty;
    }

    internal OperationsUserAuthenticatorApp(Guid tenantId, Guid userId, string encryptedSecret, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        UserId = userId;
        EncryptedSecret = encryptedSecret;
    }

    public Guid UserId { get; private set; }

    public string EncryptedSecret { get; private set; }

    public DateTimeOffset? ConfirmedUtc { get; private set; }

    public long LastAcceptedTimeStep { get; private set; }

    public bool IsConfirmed => ConfirmedUtc is not null;

    internal void Confirm(long timeStep, DateTimeOffset utcNow)
    {
        if (IsConfirmed)
        {
            throw new DomainException(OperationsIdentityErrors.AuthenticatorNotPending);
        }

        LastAcceptedTimeStep = timeStep;
        ConfirmedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    internal void RecordUse(long timeStep, DateTimeOffset utcNow)
    {
        if (!IsConfirmed)
        {
            throw new DomainException(OperationsIdentityErrors.AuthenticatorNotConfirmed);
        }

        if (timeStep <= LastAcceptedTimeStep)
        {
            throw new DomainException(OperationsIdentityErrors.AuthenticatorCodeReused);
        }

        LastAcceptedTimeStep = timeStep;
        MarkAsModified(utcNow);
    }
}
