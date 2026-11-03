using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.UseCases.Reports.RunOperationsReport;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Reports;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Reports;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsReportScheduleWorker.cs
//
// PURPOSE:
// Sends the scheduled reports (Slice 11; owner, 2026-10-22: emailed CSV or Excel to staff, not kept).
//
// RESPONSIBILITIES:
// - Every 5 minutes: the due schedules of every company; each recipient gets the report run with their own rights, in their language
// - Then the schedule moves to its next time
//
// IMPORTANT NOTES:
// - Claimed first (one host sends it); a recipient who may no longer see the report is skipped
// - Partly sent: on to the next time; nothing sent: tried again after a growing wait, then the period is skipped
// - The file is made in memory, emailed and dropped
// =========================================================
public sealed class OperationsReportScheduleWorker : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopes;

    private readonly ILogger<OperationsReportScheduleWorker> _logger;

    public OperationsReportScheduleWorker(IServiceScopeFactory scopes, ILogger<OperationsReportScheduleWorker> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // One round (also called by tests). The number of emails sent.
    public static async Task<int> RunOnceAsync(IServiceScopeFactory scopes, ILogger logger, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(logger);
        IReadOnlyList<(Guid TenantId, Guid ScheduleId)> due;

        await using (AsyncServiceScope system = scopes.CreateAsyncScope())
        {
            system.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
            due = await system.ServiceProvider.GetRequiredService<IOperationsReportScheduleRepository>().DueAsync(utcNow, 100, cancellationToken);
        }

        int sent = 0;

        foreach ((Guid tenantId, Guid scheduleId) in due)
        {
            try
            {
                sent += await SendAsync(scopes, tenantId, scheduleId, utcNow, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Scheduled report {ScheduleId} of tenant {TenantId} failed; it runs again next round.", scheduleId, tenantId);
            }
        }

        return sent;
    }

    private static async Task<int> SendAsync(IServiceScopeFactory scopes, Guid tenantId, Guid scheduleId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;
        services.GetRequiredService<IOperationsTenantScope>().Enter(tenantId);
        IOperationsReportScheduleRepository schedules = services.GetRequiredService<IOperationsReportScheduleRepository>();

        // One host only: the schedule is pushed half an hour on before anything is sent.
        if (!await schedules.ClaimAsync(scheduleId, utcNow, utcNow.AddMinutes(30), cancellationToken)
            || await schedules.GetAsync(scheduleId, cancellationToken) is not { } schedule || OperationsReportCatalog.Find(schedule.ReportKey) is not { } report)
        {
            return 0;
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings? settings = await services.GetRequiredService<IOperationsSettingsRepository>().FindAsync(cancellationToken);
        TimeZoneInfo zone = TimeZoneInfo.TryFindSystemTimeZoneById(settings?.TimeZone ?? "UTC", out TimeZoneInfo? found) ? found : TimeZoneInfo.Utc;
        // The period is the one just past on the day of the run (not of the lease).
        DateOnly runDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, zone).DateTime);
        (DateOnly from, DateOnly to) = OperationsReportScheduling.Covered(schedule.Frequency, runDay);
        string? company = (await services.GetRequiredService<GetOperationsSignInBrandUseCase>()
            .ExecuteAsync(OperationsAuthorizationContext.ForTenant(tenantId), new GetOperationsSignInBrandRequest(), cancellationToken)).DisplayName;
        IStringLocalizer<OperationsText> text = services.GetRequiredService<IStringLocalizer<OperationsText>>();
        IOperationsReportFiles files = services.GetRequiredService<IOperationsReportFiles>();
        IOperationsReportMailer mailer = services.GetRequiredService<IOperationsReportMailer>();
        int sent = 0;
        int failed = 0;

        foreach (Guid recipient in schedule.Recipients)
        {
            if (await services.GetRequiredService<IOperationsUserRepository>().GetAsync(recipient, cancellationToken) is not { Account.IsActive: true } person)
            {
                continue;
            }

            (CultureInfo culture, CultureInfo uiCulture) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);

            try
            {
                // The recipient's own rights decide what they get (and whether they get it).
                RunOperationsReportResponse run = await services.GetRequiredService<RunOperationsReportUseCase>().ExecuteAsync(
                    OperationsAuthorizationContext.ForScheduledWork(tenantId, recipient), new RunOperationsReportRequest
                    {
                        Key = report.Key,
                        From = from,
                        To = to,
                        Grain = Enum.IsDefined((OperationsReportGrain)schedule.Grain) ? (OperationsReportGrain)schedule.Grain : OperationsReportGrain.Day,
                        Dimension = schedule.Dimension is { } dimension && Enum.IsDefined((OperationsReportDimension)dimension) ? (OperationsReportDimension)dimension : null,
                        WarehouseId = schedule.WarehouseId,
                        Days = schedule.Days
                    }, cancellationToken);

                if (!run.Succeeded)
                {
                    continue;
                }

                CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(person.PreferredLanguage.Value);
                OperationsReportSheet sheet = OperationsReportWords.Sheet(text, run, company);
                bool excel = schedule.Format == OperationsReportFormat.Xlsx;
                string name = report.Key + "-" + OperationsReportPeriods.Iso(run.From) + "-" + OperationsReportPeriods.Iso(run.To) + (excel ? ".xlsx" : ".csv");
                await mailer.SendAsync(new OperationsReportMail(person.Email.Value, person.FullName, person.PreferredLanguage, company ?? string.Empty, sheet.Title, sheet.Notes[1], name,
                    excel ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "text/csv", excel ? files.Xlsx(sheet) : OperationsReportCsv.Write(sheet)), cancellationToken);
                sent++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failed++;
                services.GetRequiredService<ILogger<OperationsReportScheduleWorker>>().LogWarning(exception, "A scheduled report could not be emailed (schedule {ScheduleId}).", scheduleId);
            }
            finally
            {
                (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, uiCulture);
            }
        }

        // Nothing could be mailed at all: tried again after a growing wait; otherwise on to the next time.
        DateTimeOffset next = OperationsReportScheduling.NextRun(schedule.Frequency, utcNow, zone);

        if (sent == 0 && failed > 0)
        {
            schedule.Failed(next, utcNow);
        }
        else
        {
            schedule.Sent(next, utcNow);
        }

        IOperationsUnitOfWork unitOfWork = services.GetRequiredService<IOperationsUnitOfWork>();

        if (!await unitOfWork.SaveChangesAsync(cancellationToken))
        {
            unitOfWork.DiscardChanges();
        }

        return sent;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Interval);

        do
        {
            try
            {
                await RunOnceAsync(_scopes, _logger, DateTimeOffset.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Scheduled reports round failed; it will run again.");
            }
        }
        while (await NextAsync(timer, stoppingToken));
    }

    private static async Task<bool> NextAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
