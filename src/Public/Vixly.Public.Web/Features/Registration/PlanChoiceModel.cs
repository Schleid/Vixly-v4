namespace Vixly.Public.Web.Features.Registration;

// =========================================================
// FILE: PlanChoiceModel.cs
//
// PURPOSE:
// What the plan step posts.
//
// RESPONSIBILITIES:
// - Hold the plan, term, licences, trial and promotion code
//
// IMPORTANT NOTES:
// - Platform checks the plan, limits and promotion
// =========================================================
public sealed class PlanChoiceModel
{
    public string? PlanCode { get; set; }

    public string Term { get; set; } = "monthly";

    public int Licences { get; set; } = 1;

    public bool StartTrial { get; set; }

    public string? PromotionCode { get; set; }
}
