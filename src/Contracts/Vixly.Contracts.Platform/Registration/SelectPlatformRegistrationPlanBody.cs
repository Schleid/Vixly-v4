namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: SelectPlatformRegistrationPlanBody.cs
//
// PURPOSE:
// The plan a registering company chooses.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Term is monthly or annual
// - Validated by Platform; nothing here is trusted
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record SelectPlatformRegistrationPlanBody
{
    public string PlanCode { get; init; } = string.Empty;

    public string Term { get; init; } = string.Empty;

    public int Licences { get; init; }

    public bool StartTrial { get; init; }

    public string? PromotionCode { get; init; }

    public string? OfferToken { get; init; }
}
