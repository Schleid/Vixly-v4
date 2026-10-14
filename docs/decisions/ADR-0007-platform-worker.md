# ADR-0007: Platform background jobs run in a Worker deployable

- Status: **Accepted** (owner decision, 2026-10-13: Windows Service)
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
