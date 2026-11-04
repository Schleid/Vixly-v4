# Slice 2: Tenants - verification record

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Slice | 2, Tenants ([design note](SLICE-2-TENANTS-DESIGN.md), [V3 sweep](SLICE-2-TENANTS-SWEEP.md)) |
| Prepared | 2026-10-14 |
| Status | **Signed off by the owner, 2026-10-14, with one correction for later** (section 5) |

## 1. Gate (plane document section 8)

| Gate item | Result | Evidence |
|---|---|---|
| Builds with no warnings | Pass | `dotnet build Vixly.Platform.slnx -c Release`: 0 warnings, 0 errors |
| Architecture tests pass | Pass | 2,285 checks, including the new Api project, Contracts and the extended audit-code scan (`RecordChange`, `RecordMove`) |
| Unit tests for Domain and Application | Pass | Domain 168 (102 new), Application 134 (33 new), Kernel 81 |
| Authorization tests | Pass | Every use case goes through the gate. Tests cover: create, notes and step-up refusals; ban without SuperUser; abilities |
| Tenant isolation tests | Not applicable yet | Staff reads span all tenants by design. Tenant-scoped reads arrive with `Contracts.Platform` (Slice 9). The registration access token is scoped to one registration, and a rotated token is refused (tested) |
| Anonymous endpoints listed | Pass | Design note section 6 and the Platform.Api README, with rate limits |
| en, fr and es complete | Pass | Same keys, no empty value, matching placeholders (architecture rules). fr and es are machine-drafted and `needs-review` |
| Slice design note in `docs/platform/` | Pass | `SLICE-2-TENANTS-DESIGN.md`, with progress and decisions taken while building |
| Owner signs this record | Pending | Section 5 |

**Total: 2,715 automated checks green (Release).** Breakdown:

- Architecture 2,285
- Domain 168
- Application 134
- Kernel 81
- Infrastructure 28, of which 5 run against SQL Server
- Web 11
- Api 8

## 2. "Done when" list (design note section 8)

| Item | Result | Evidence |
|---|---|---|
| Every lifecycle move tested from every status, allowed and refused | Pass | `PlatformTenantLifecycleTests` (25 cases). Pending activation is reached only in Slice 5 |
| One event and one audit entry per move, in the same commit | Pass | `Suspend_NeedsStepUp_ThenRecordsHistoryAuditAndOutbox_WithoutTheReason`; the database test proves a failed save leaves nothing |
| Slug correctable only while never active, including after deactivation | Pass | `Slug_IsFixed_OnceTheTenantHasBeenActiveOrIsClosed`, `CorrectSlug_MovesTheClaim_OnlyWhileNeverActive` |
| A slug is never reused | Pass | Permanent claims are never removed; deactivated and banned tenants keep theirs (domain tests) |
| A slug can never belong to two tenants | Pass | Domain test; `TwoRacingClaims_ForOneSlug_OnlyOneCommits` against SQL Server |
| Reserved slugs refused, H-2 floor included | Pass | `ReservedFloor_CannotBeRemovedByConfiguration`, `Create_RefusesUnusableSlugs` |
| Verification link single-use, 24 hours, hash only | Pass | `VerificationLink_WorksOnce_AndNotAfterExpiry`, `Start_ClaimsTheSlug_StoresOnlyHashes_AndSendsALink` |
| Status endpoint reveals only the next step | Pass | `GetMyRegistrationStatus` returns step, masked email, slug and deadline only |
| Rate-limit, body-size and header tests on Platform.Api | Pass | `PlatformApiHttpTests` (8) |
| Every tenant query paged; the list loads only what it shows | Pass | `ListAsync` projections; `Directory_FindsByNamePhoneDigitsAndTag` |
| Outbox: failed commit leaves no message; no double handling; two dispatchers never share a message | Pass | `FailedSave_LeavesNoOutboxMessage`, `Leasing_GivesEachMessageOnce_OldestFirstPerStream`, `Dispatcher_SendsNoticesOnce_ToOwnerAndBilling_WithoutTheReason` |
| A notice never carries the internal reason | Pass | Handler passes status and category only (application test). The rendered French email was checked end to end. en and es use the same keys and values |
| Registration needs accepted, current terms; versions kept | Pass | `Start_NeedsTheCurrentTerms`; consent copied to tenant history |

## 3. End-to-end run (2026-10-14, SQL Server, database built from the global script)

The run used the console as a SuperUser, Platform.Api over HTTPS, and the app login (not `sa`).

| # | Step | Result |
|---|---|---|
| 1 | New tenant from the console, French, Toronto, address preview | Draft `TEN-CWCRQF`, `northwnd.techcrafters.test` |
| 2 | Correct the address while Draft | `northwind.techcrafters.test`, history "Address corrected from northwnd to northwind" |
| 3 | Activate | Active; the address section disappears (fixed for good) |
| 4 | Billing contact, account manager, tags | Saved and shown in the directory |
| 5 | Suspend (Security) with an internal reason | Two notices in French, to owner and billing, category only. The reason was not in the email |
| 6 | Reactivate, add a note | History reads newest first in English and in French |
| 7 | Directory search by phone digits "514 555" plus tag | One match |
| 8 | Public API: taken and free slug, start (es), verify from the emailed link | `pending_plan_selection`, Draft tenant `TEN-DB43QA`; the old access token and a reused link are refused |
| 9 | Console registrations: open, cancel with a reason | The slug is free again at once |
| 10 | Audit log | Tenant and registration entries named |
| 11 | App login tries `UPDATE` on tenant history | Denied |
| 12 | Outbox | Every message processed |

## 4. Known open items

| Item | Plan |
|---|---|
| Public.Web registration pages (the API is ready) | Stage 2 |
| Plan selection, payment, account setup, first device | Slices 3, 5, Operations, 7 (D-T4) |
| Subscription reacts to tenant status messages | Slice 3 (D-T6), as a second outbox handler |
| Outbox dispatcher moves to the Worker; expired registrations swept in bulk | Slice 4 |
| Tenant activity counts from Operations | Stage 3 (D-T14) |
| French and Spanish native review | Before release |
| Console layout and styling of the new screens | Frontend/UI (FE report to be updated) |
| Turnstile site key and secret for staging and production | Owner or operations, through user secrets or the host |
| Replace staff tenant creation with an invitation link; make company legal data read-only in the console (the slug correction before first activation stays); the tenant edits its profile in Operations | Done 2026-10-16: invite-only registration (B11); legal data read-only with change requests (B12, design note "Company change requests"). The Operations profile screen comes with Stage 3 |
| Tenant page: subscription, finance, licensing and activity sections | Slices 3, 4, 5, 7 and Stage 3; see the design note |
| Demo data for development | Done: `dotnet run -- --seed-demo-tenants` (Development only), built through registrations |

## 5. Owner sign-off

| | |
|---|---|
| Decision | Approved |
| Signed by | Jude (owner) |
| Date | 2026-10-14 |
| Correction for later | **Tenant data comes from registration, not entered by staff.** This revises D-T8. The console's "New tenant" form (`CreatePlatformTenant`) is to be replaced in a later slice; tracked in the design note, decision D-T8. **Done 2026-10-16:** the form and use case are removed; registration is invite-only through Sales offer links until public sign-up opens |
