using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformBillingTextItem.cs
//
// PURPOSE:
// Payment instructions and footer for one language, as shown and edited.
//
// RESPONSIBILITIES:
// - Carry what the console shows
//
// IMPORTANT NOTES:
// - Plain record for the console
// =========================================================
public sealed record PlatformBillingTextItem(string? PaymentInstructions, string? Footer);
