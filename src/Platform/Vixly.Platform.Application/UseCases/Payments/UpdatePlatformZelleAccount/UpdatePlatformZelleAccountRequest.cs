namespace Vixly.Platform.Application.UseCases.Payments.UpdatePlatformZelleAccount;

// =========================================================
// FILE: UpdatePlatformZelleAccountRequest.cs
//
// PURPOSE:
// Saves Vixly's Zelle details and turns Zelle at checkout on or off.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdatePlatformZelleAccountRequest
{
    public bool Enabled { get; init; }

    public string? RecipientName { get; init; }

    public string? Email { get; init; }

    public string? Tag { get; init; }

    public string? Phone { get; init; }
}
