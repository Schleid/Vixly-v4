namespace Vixly.Shared.Hosting.Settings;

// =========================================================
// FILE: VixlySettingsReport.cs
//
// PURPOSE:
// The result of the settings check of one site (settings check, 2026-10-23).
//
// RESPONSIBILITIES:
// - Hold the problems found, the blocking ones first
// - Hold the optional settings the owner chose to go without (VixlySettingsCheck:Waived, 2026-10-25)
//
// IMPORTANT NOTES:
// - Registered as a singleton, read by the health check and the start-up log
// =========================================================
public sealed class VixlySettingsReport
{
    public VixlySettingsReport(string site, IReadOnlyList<VixlySettingProblem> problems, IReadOnlyList<string>? waived = null)
    {
        Site = site;
        Waived = (waived ?? Array.Empty<string>()).Order(StringComparer.Ordinal).ToArray();
        Problems = problems.OrderByDescending(problem => problem.Blocks).ThenBy(problem => problem.Rule.Key, StringComparer.Ordinal).ToArray();
    }

    public string Site { get; }

    public IReadOnlyList<VixlySettingProblem> Problems { get; }

    public IReadOnlyList<VixlySettingProblem> Blocking => Problems.Where(problem => problem.Blocks).ToArray();

    // Optional settings left out on purpose: not a problem, but still named on the detailed health page.
    public IReadOnlyList<string> Waived { get; }

    public IReadOnlyList<VixlySettingProblem> Warnings => Problems.Where(problem => !problem.Blocks).ToArray();
}
