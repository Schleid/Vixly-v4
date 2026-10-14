# Slice 1: Identity and access - verification record

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Slice | 1, Identity and access ([design note](SLICE-1-IDENTITY-DESIGN.md), [V3 sweep](SLICE-1-IDENTITY-SWEEP.md)) |
| Prepared | 2026-10-14 |
| Status | **Signed off by the owner, 2026-10-14** |

## 1. Gate (plane document section 8)

| Gate item | Result | Evidence |
|---|---|---|
| Builds with no warnings | Pass | `dotnet build Vixly.Platform.slnx -c Release`: 0 warnings, 0 errors (warnings are errors), .NET SDK 10.0.402 |
| Architecture tests pass | Pass | 1,564 checks: layering, use-case shape, file headers and encoding, UI rules UI-6, UI-7, UI-10, resource completeness, audit action catalogue |
| Unit tests for Domain and Application | Pass | Domain 66, Application 101, Kernel 81, Infrastructure 23 |
| Authorization tests | Pass | Every use case goes through the gate (architecture rule); refusals tested per area (users, roles, departments, audit) |
| Tenant isolation tests | Not applicable | Slice 1 has no tenant data; starts with Slice 2 |
| Anonymous endpoints listed | Pass | Design note section 5, with rate limits |
| en, fr and es complete | Pass | Same keys, no empty value, matching placeholders in all three (architecture rules). fr and es are machine-drafted and marked `needs-review` for a native reviewer |
| Slice design note in `docs/platform/` | Pass | `SLICE-1-IDENTITY-DESIGN.md`, with progress log and the decisions taken while building |
| Owner signs this record | Pass | Section 5 |

Total: 1,844 automated checks green (Release).

## 2. "Done when" list (design note section 8)

| Item | Result | Evidence |
|---|---|---|
| Every staff sign-in needs a second factor; a password alone never yields a session | Pass | `SignInFlowTests.PasswordAlone_NeverYieldsASession` (added in this sweep) and `Password_WithoutSecondFactor_LeadsToEnrolment_NotToASession` |
| Lower-cost (V3) BCrypt hashes verify and are upgraded to cost 12 on sign-in | Pass | `PlatformSecurityAdapterTests.BCrypt_LowerCostHash_FromV3_VerifiesAndAsksForUpgrade`; sign-in rehashes on `SucceededNeedsRehash` |
| Lock, suspend and deactivate end the target's sessions at once | Pass, see note | Application tests (sessions end, one audit row, one commit); browser run against SQL Server: the locked user's next click lands on sign-in. Note: no automated database integration test yet (section 3) |
| Last-SuperUser and self-action rules have domain tests | Pass | `PlatformUserTests`, `PlatformUserAuthorityLevelTests`; role and department use cases add own-account and higher-authority refusals |
| Every command writes exactly one audit entry in the same commit (D-2) | Pass | Lifecycle, roles, departments and account tests assert the audit rows and the single save; the app login has no UPDATE or DELETE on `platform.AuditEntries` (database script) |
| Login CSRF, lockout and rate-limit tests; response headers test on `Platform.Web` | Pass | New `PlatformConsoleHttpTests` run the real pipeline in memory at the production host name: sign-in without the antiforgery token is refused (400); the 11th sign-in post in a minute gets 429; security headers present (CSP with `frame-ancestors 'none'` and `script-src 'self'`, `nosniff`, `DENY`, referrer policy); a protected page redirects to sign-in. Lockout: domain tests (5 failures, 15 minutes) and `SignInFlowTests` |
| en, fr, es complete for every string and error | Pass | Resource rules above; every declared error code has text |

## 3. Known open items (do not block the gate)

1. **Database integration tests.** Repositories and queries are proven by browser runs against SQL Server, not by automated tests. Recommended: a CI job with a SQL Server container running the repository and query tests. Needs Docker on the build agent.
2. **Clean-up of expired rows** (sign-in challenges, passkey ceremonies, reset-request log, ended sessions). Belongs to the Platform Worker, Slice 4 (ADR-0007). Until then the tables only grow slowly; no security effect, since every row expires.
3. **Native review of French and Spanish** (`needs-review` comments in the resource files).
4. **Console design.** Placeholder styling until the Frontend/UI team delivers (approved brief, 2026-10-14).

## 4. Owner verification steps (on your PC)

```
dotnet build Vixly.Platform.slnx -c Release
dotnet test  Vixly.Platform.slnx -c Release
```

Expected: 0 warnings, all tests green. Then, in the running console:

1. Sign in, open **Roles**: two system roles exist (Security administrator, Administrator).
2. Invite a test user; accept the invitation in a private window and choose **Create a passkey** (or the authenticator app).
3. Give that user a role until a date; sign in as them and check what they can see; remove the role.
4. Open **Audit log** and find each of those actions.

## 5. Sign-off

| Check | Result |
|---|---|
| Builds warning-free | Accepted |
| All tests pass | Accepted |
| Console walk-through (section 4) | Accepted |
| Owner | Jude (approved in the build conversation) |
| Date | 2026-10-14 |
