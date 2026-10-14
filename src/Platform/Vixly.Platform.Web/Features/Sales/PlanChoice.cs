namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: PlanChoice.cs
//
// PURPOSE:
// A plan to pick in a sales form.
//
// RESPONSIBILITIES:
// - Hold what its component shows
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record PlanChoice(Guid PlatformPlanId, string Code, string Name, string CurrencyCode);
