namespace Vixly.Shared.Hosting.Settings;

// =========================================================
// FILE: VixlySettingProblem.cs
//
// PURPOSE:
// A setting that is missing or wrong (settings check, 2026-10-23).
//
// RESPONSIBILITIES:
// - Carry the rule and what is wrong, in words for the operator
//
// IMPORTANT NOTES:
// - Never carries the value: a secret must not reach a log or a page
// =========================================================
public sealed record VixlySettingProblem(VixlySettingRule Rule, string Problem)
{
    public bool Blocks => Rule.Required;

    public override string ToString() => Rule.Key + " (" + Rule.EnvironmentName + "): " + Problem + ". " + Rule.Purpose;
}
