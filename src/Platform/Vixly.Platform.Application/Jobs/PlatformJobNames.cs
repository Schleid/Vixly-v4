namespace Vixly.Platform.Application.Jobs;

// =========================================================
// FILE: PlatformJobNames.cs
//
// PURPOSE:
// The names of Control's background jobs, as recorded in JobRuns and shown on the Jobs page.
//
// RESPONSIBILITIES:
// - Name each step of the subscription clock once
//
// IMPORTANT NOTES:
// - A name is stored: never rename a shipped one
// - The Jobs page shows ui.jobs.job.<name>
// =========================================================
public static class PlatformJobNames
{
    public const string Subscriptions = "subscriptions";

    public const string Billing = "billing";

    public const string Payments = "payments";

    public const string Collections = "collections";

    public const string Licensing = "licensing";

    public const string Legal = "legal";

    public const string EventFeed = "event_feed";

    public const string Leads = "leads";

    public const string SignInCleanup = "sign_in_cleanup";

    public const string JobRunCleanup = "job_run_cleanup";

    // Owner, 2026-10-28: the maintenance windows sent again to the Cloudflare Worker, every hour.
    public const string EdgeSync = "edge_sync";

    // Sales feed to TechOS (2026-11-02): every minute, by its own worker.
    public const string SalesFeed = "sales_feed";

    // A reminder 7 days before an annual renewal charged to a saved card (2026-11-02).
    public const string RenewalReminders = "renewal_reminders";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Subscriptions,
        Billing,
        Payments,
        Collections,
        Licensing,
        Legal,
        EventFeed,
        Leads,
        SignInCleanup,
        JobRunCleanup,
        EdgeSync,
        SalesFeed,
        RenewalReminders
    };
}
