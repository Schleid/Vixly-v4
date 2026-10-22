using Microsoft.AspNetCore.DataProtection;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Security;

// =========================================================
// FILE: OperationsSecretProtector.cs
//
// PURPOSE:
// Protects short secrets (the pickup code) with ASP.NET Core Data Protection (Slice 7 step 2 review).
//
// RESPONSIBILITIES:
// - Protect; read back, or nothing when the value was changed or the key is gone
//
// IMPORTANT NOTES:
// - One purpose for pickup codes; the keys are kept apart from the data
// =========================================================
internal sealed class OperationsSecretProtector : IOperationsSecretProtector
{
    private readonly IDataProtector _protector;

    public OperationsSecretProtector(IDataProtectionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _protector = provider.CreateProtector("Vixly.Operations.PickupCode.v1");
    }

    public string Protect(string value) => _protector.Protect(value ?? throw new ArgumentNullException(nameof(value)));

    public string? Unprotect(string protectedValue)
    {
        try
        {
            return _protector.Unprotect(protectedValue);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }
}
