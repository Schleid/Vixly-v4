namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsSecretProtector.cs
//
// PURPOSE:
// Protect a short secret with the server's key, so that a copy of the database alone never gives it (Slice 7 step 2 review).
//
// RESPONSIBILITIES:
// - Protect a value; read a protected value back, or nothing when it was tampered with
//
// IMPORTANT NOTES:
// - Backed by ASP.NET Core Data Protection, whose keys are kept apart from the data
// =========================================================
public interface IOperationsSecretProtector
{
    string Protect(string value);

    string? Unprotect(string protectedValue);
}
