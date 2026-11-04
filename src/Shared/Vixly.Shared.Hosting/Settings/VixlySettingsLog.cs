using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Vixly.Shared.Hosting.Settings;

// =========================================================
// FILE: VixlySettingsLog.cs
//
// PURPOSE:
// Writes the settings check to the log when the site starts (settings check, 2026-10-23).
//
// RESPONSIBILITIES:
// - One warning line per setting the site runs without
//
// IMPORTANT NOTES:
// - Names and reasons only, never values
// =========================================================
public sealed class VixlySettingsLog : IHostedService
{
    private readonly VixlySettingsReport _report;

    private readonly ILogger<VixlySettingsLog> _logger;

    public VixlySettingsLog(VixlySettingsReport report, ILogger<VixlySettingsLog> logger)
    {
        _report = report ?? throw new ArgumentNullException(nameof(report));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (VixlySettingProblem problem in _report.Problems)
        {
            _logger.LogWarning("Setting {Problem}", problem.ToString());
        }

        foreach (string key in _report.Waived)
        {
            _logger.LogInformation("Setting {Key} is waived on purpose ({Section}).", key, VixlySettingsCheck.WaivedSection);
        }

        if (_report.Problems.Count == 0)
        {
            _logger.LogInformation("Settings of {Site}: OK.", _report.Site);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
