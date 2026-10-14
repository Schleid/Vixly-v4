namespace Vixly.Platform.Application.UseCases.Plans.CreatePlatformPlan;

// =========================================================
// FILE: CreatePlatformPlanRequest.cs
//
// PURPOSE:
// Creates a plan with its first draft version (D-S1, D-S2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreatePlatformPlanRequest
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string CurrencyCode { get; init; } = string.Empty;
}
