namespace Vixly.Platform.Domain.Legal;

// =========================================================
// FILE: PlatformLegalRules.cs
//
// PURPOSE:
// When a tenant must accept the Terms, and which reminder is due (D-Lg6 to D-Lg10).
//
// RESPONSIBILITIES:
// - Deadline: the version deadline, or 30 days after the tenant was first activated when later
// - Status: accepted, due or overdue
// - The automatic reminder step due now, if any
//
// IMPORTANT NOTES:
// - Pure functions; the clock and the use cases call them
// =========================================================
public static class PlatformLegalRules
{
    public static DateTimeOffset Deadline(PlatformLegalDocument required, DateTimeOffset? tenantActivatedUtc)
    {
        ArgumentNullException.ThrowIfNull(required);
        DateTimeOffset deadline = required.DeadlineUtc ?? required.EffectiveUtc ?? required.CreatedUtc;
        DateTimeOffset grace = (tenantActivatedUtc ?? deadline).AddDays(PlatformLegalLimits.NewTenantDays);
        return tenantActivatedUtc is { } activated && activated > (required.PublishedUtc ?? deadline) && grace > deadline ? grace : deadline;
    }

    public static PlatformLegalStatus Status(bool asked, bool accepted, DateTimeOffset deadline, DateTimeOffset utcNow)
    {
        return !asked ? PlatformLegalStatus.NotRequired
            : accepted ? PlatformLegalStatus.Accepted
            : utcNow >= deadline ? PlatformLegalStatus.Overdue
            : PlatformLegalStatus.Due;
    }

    // The latest step whose time has come and that was not sent; only that one is sent after downtime.
    public static PlatformTermsReminderStep? DueStep(DateTimeOffset publishedUtc, DateTimeOffset deadline, IReadOnlyCollection<PlatformTermsReminderStep> sent, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(sent);
        (PlatformTermsReminderStep Step, DateTimeOffset At)[] plan =
        {
            (PlatformTermsReminderStep.Published, publishedUtc),
            (PlatformTermsReminderStep.Before14Days, deadline.AddDays(-14)),
            (PlatformTermsReminderStep.Before7Days, deadline.AddDays(-7)),
            (PlatformTermsReminderStep.Before1Day, deadline.AddDays(-1)),
            (PlatformTermsReminderStep.After1Week, deadline.AddDays(7)),
            (PlatformTermsReminderStep.After2Weeks, deadline.AddDays(14)),
            (PlatformTermsReminderStep.After3Weeks, deadline.AddDays(21)),
            (PlatformTermsReminderStep.After4Weeks, deadline.AddDays(28))
        };

        (PlatformTermsReminderStep Step, DateTimeOffset At)[] reached = plan.Where(item => item.At <= utcNow && item.At >= publishedUtc).ToArray();

        if (reached.Length == 0)
        {
            return null;
        }

        PlatformTermsReminderStep latest = reached[^1].Step;
        return sent.Any(step => step != PlatformTermsReminderStep.Manual && step >= latest) ? null : latest;
    }
}
