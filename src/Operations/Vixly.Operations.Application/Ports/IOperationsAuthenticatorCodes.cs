namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsAuthenticatorCodes.cs
//
// PURPOSE:
// Authenticator app (TOTP) secrets and codes.
//
// RESPONSIBILITIES:
// - Create and reopen an enrolment
// - Check a code and return its time step
//
// IMPORTANT NOTES:
// - The issuer shown in the app is the tenant display name, never "Vixly" (ADR-0012)
// =========================================================
public interface IOperationsAuthenticatorCodes
{
    OperationsAuthenticatorEnrolment CreateEnrolment(string issuer, string accountLabel);

    OperationsAuthenticatorEnrolment? Reopen(string encryptedSecret, string issuer, string accountLabel);

    long? Verify(string encryptedSecret, string code, DateTimeOffset utcNow);
}
