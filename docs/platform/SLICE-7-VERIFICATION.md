# Slice 7: Licensing - verification record

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Date | 2026-10-16 |
| Built on | Slices 5 and 6 as delivered (not committed: the dev team owns git) |
| Design | [Design note](SLICE-7-LICENSING-DESIGN.md) (build notes in section 10), [sweep](SLICE-7-LICENSING-SWEEP.md) |
| Status | **Built and verified; waiting for owner sign-off** |

## 1. Automated checks

Release build of `Vixly.Platform.slnx`: no errors, no warnings.

| Test project | Before (Slice 6) | After |
|---|---|---|
| Platform.Domain.Tests | 268 | 277 |
| Platform.Application.Tests | 189 | 198 |
| Platform.Infrastructure.Tests (with SQL Server) | 50 | 52 |
| Platform.Api.Tests | 8 | 8 |
| Platform.Web.Tests | 31 | 31 |
| Architecture.Tests | 4,752 | 5,116 |
| Shared.Kernel.Tests | 81 | 81 |

All pass.

What the new tests cover:
- **Seats:** approval takes a free seat and never more than bought; a third device on two licences gets "No licence is free"; approvals need full subscription access.
- **Tenant isolation:** another tenant's device is never found, by approval or by check.
- **Rotating secret:** valid check; wrong secret refused; rotation after 24 hours; the new secret works; the old one used after the 5-minute grace revokes the device, frees the seat and emails the owner.
- **Device key:** a real P-256 key signs a fresh challenge; a forged signature fails and burns the challenge; a challenge works once; an invalid key is refused when the device is added.
- **Releases:** two within the allowance are immediate; the third goes to review; a lost device stops at once and keeps its seat for review; staff need the release permission; approving frees the seats and emails the owner.
- **First-device link:** refused before payment; sent once ready; the link from the email adds the device, completes the registration and activates the tenant in one commit; the same link cannot be used twice.
- **Owner recovery:** needs manage; with every seat taken the owner must name a device to replace; the replaced device is revoked and the owner is told.
- **Ending:** a closed account revokes every device and frees the seats; the clock expires requests older than 7 days.
- **Portal access:** unknown portal names are not found; a live tenant reports its access level.
- **Database:** seats, device, history, challenge and link round-trip; the read models find them; a second device with the same number and a second seat row are refused; two hosts taking the last seat at once: only one commits.

`DeviceEvents` is append-only in the global script (DENY UPDATE, DELETE to the app login).

## 2. End-to-end run (console, fresh Development database)

Fresh database from the global script, first SuperUser (Jude), `--seed-demo-tenants`. The clock ran every minute.

| # | Check | Result |
|---|---|---|
| 1 | Demo data | 30 devices: approved, waiting and one release under review |
| 2 | Devices page and review queue | Paged list with number, name, kind, tenant, status, requested and last seen; "Releases to review" shows the one waiting |
| 3 | Approve the release (step-up) | Device Released; seats in use 2 to 1; email "Device release approved" to the owner in the tenant's language (French); the internal note is not in the email; history shows the decision, the reason and the note |
| 4 | Revoke a device (step-up) | Device Revoked for security; seat freed |
| 5 | Tenant page, Licensing section | Licences in use, waiting, under review, release allowance, link to the tenant's devices |
| 6 | Start owner recovery (step-up) | "Recover access" email to the owner with a one-time link on the tenant's portal address; the link is never on screen; one open link stored as a hash |
| 7 | Waiting request older than 7 days | Expired by the clock |
| 8 | History protection | The app login cannot update device history (permission denied) |
| 9 | Menu and outbox | "Devices" under Commercial; no failing outbox message |

Screens were captured for the device list, a device under review and the tenant Licensing section.

## 3. Not covered here

- The Operations screens that add, approve and release devices, and the portal pages for the owner links, belong to the Operations plane; Slice 9 exposes the calls. They are covered by the application tests above.
