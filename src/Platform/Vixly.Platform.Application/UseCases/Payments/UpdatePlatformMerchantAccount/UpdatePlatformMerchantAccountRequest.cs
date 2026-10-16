namespace Vixly.Platform.Application.UseCases.Payments.UpdatePlatformMerchantAccount;

// =========================================================
// FILE: UpdatePlatformMerchantAccountRequest.cs
//
// PURPOSE:
// Changes the publishable key, accepted currencies and whether card payments are on (D-P8, D-P11).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdatePlatformMerchantAccountRequest
{
    public string? PublishableKey { get; init; }

    public IReadOnlyList<string> Currencies { get; init; } = Array.Empty<string>();

    public bool Enabled { get; init; }
}
