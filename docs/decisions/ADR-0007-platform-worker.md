# ADR-0007: Platform background jobs run in a Worker deployable

- Status: **Accepted** (owner decision, 2026-10-13: Windows Service). **Amended 2026-10-22** (Amendment 1: jobs run in the web apps on shared hosting)
- Date: 2026-10-13
- Deciders: Jude (owner)
- Affects: Global Standard sections 1, 2, 3; Platform plane (Slice 4 onward)

## Context

V3 runs recurring billing and collections as hosted services inside the IIS-hosted Blazor process. IIS recycles and idles that process, jobs share its memory and failures are only visible in log files. The V3 collections defect went unnoticed for 41 days because a per-tenant failure was swallowed and no run record existed.

## Decision

- Platform background jobs run in `Vixly.Platform.Worker`, a .NET Worker deployable installed as a Windows Service. No job runs inside `Platform.Web` or `Platform.Api`.
- Scheduling is SQL-backed (Quartz.NET or Hangfire, chosen in Slice 4) with its store in the `platform` schema, so only one instance runs a job at a time.
- Every run writes a `PlatformJobRun` row: job, start, end, outcome, items processed, items failed, correlation id. A failed item counts as a failure (D-6) and raises an alert to the console and by email.
- The console shows job runs and offers "run now" behind a permission.
- The Worker references its own `Application` and, at the composition root only, `Infrastructure`, like the Api and Web hosts.

## Consequences

- One more deployable to install and monitor; it exposes `/health/live` and `/health/ready` (S-12).
- Recurring billing (Slice 4), collections (Slice 6) and tenant activity pulls (Slice 2 / Stage 3) are built as Worker jobs from the start.

## Amendment 1 (2026-10-22): jobs run in the web apps on shared hosting

Owner decision, 2026-10-21: V4 production is hosted at SmarterASP.net, as V3 is. Shared hosting cannot run a Windows Service, and the Worker was never built. The project document *Background jobs: V3 compared with V4* records the comparison.

**Decision**

- Platform jobs run as hosted services in `Platform.Web` (`PlatformOutboxWorker`, `PlatformSubscriptionClockWorker`), and Operations jobs in `Operations.Web`. `Vixly.Platform.Worker` stays the later option. The jobs are separate Application classes, so moving them needs no rewrite.
- Every job is safe to run twice and after a gap. Each acts only on what is due, and claims, row versions and database constraints guard each item. A recycle or a second copy costs nothing.
- The sites are kept awake, by the hosting's "always running" setting or by an uptime ping to `/health/live` every 5 minutes. An idle app pool stops the jobs.
- Expired sign-in rows are removed every hour (2026-10-22):
  - sessions a year after they expired, since they are the sign-in history;
  - challenges, ceremonies and links 30 days after they expired.
  - Platform runs this in the subscription clock; Operations runs `OperationsHousekeepingWorker`, recorded in `ops.JobRuns`.

**Still owed from the original decision**

- A Platform run record (`PlatformJobRun`), with its alert and the console's "run now". Today Platform jobs write logs only; Operations has `ops.JobRuns`.
  - **Update 2026-10-23:** `PlatformJobRun` is built (`platform.JobRuns`, 90 days) with Control's Jobs page; each clock step runs and is recorded on its own. The alert and *Run now* remain for the Worker ([Control: job records and maintenance](../platform/CONTROL-JOBS-AND-MAINTENANCE-2026-10-23.md)).
- Pausing billing and collections during a maintenance window. This is designed with the maintenance windows item of the Control backlog (2026-10-22).
