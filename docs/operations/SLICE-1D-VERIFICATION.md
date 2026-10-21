# Slice 1D: Staff, roles and step-up - verification record

| | |
|---|---|
| Plane | Operations (Stage 3) |
| Date | 2026-10-18 |
| Built on | Slice 1C (this delivery); not committed here: the dev team owns git |
| Design | [Slice 1 design note](SLICE-1-STAFF-AND-SETUP-DESIGN.md) sections 3 (Accounts and roles), 4 (Permissions) and 11, owner decisions D-O2, D-O5 and D-O14 |
| Status | **Built and verified by automated tests; waiting for owner sign-off.** Slice 1E (settings) follows |

## 1. What was built

### Permissions and roles

- **Catalogue in code:** the 32 codes of design section 4 (`ops.staff.*`, `ops.roles.*`, `ops.devices.*`, `ops.settings.*`, `ops.company.*`, `ops.branches.*`, `ops.warehouses.*`, `ops.services.*`, `ops.brand.*`, `ops.audit.*`).
- **System roles:**
  - **Owner:** everything; protected.
  - **Administrator:** everything except the company change request. Created the first time the roles are listed; cannot be renamed, changed or deactivated.
  - Device approval and release, company contacts and the company change request belong to the system roles only. A tenant role can never hold them, because Platform accepts them from `owner` or `admin` only.
- **Tenant roles** (`/staff/roles`):
  - a name (unique per company, 60 characters at most) and permissions chosen per area;
  - deactivate and reactivate;
  - the list shows how many people hold each role.
- **Grants with scope:** a role is given for the whole company, one branch (and its warehouses), or one warehouse.
  - A person can give only roles whose every permission they hold, and only where they hold it.
  - Only the Owner gives the Administrator role.
  - Giving or removing the Administrator role sets the person's Administrator flag.
- **What a scoped grant gives** (after the review in the [Slice 1E record](SLICE-1E-VERIFICATION.md), section 3): staff, roles, settings, company, history and devices need a grant for the whole company. A branch or warehouse grant gives its permissions for that place only.
- **Scope in the data:** the branch and warehouse lists show only what the person's scope covers. Saving a warehouse needs it in scope. Moving it to another branch needs that branch in scope too.
- The site and location pages of Slice 1B now check permissions (`ops.brand.*`, `ops.branches.manage`, `ops.warehouses.manage`) instead of "Owner only". The Devices page checks `ops.devices.*`.

### Staff (`/staff/people`)

- **List:** search by name or exact email, 25 per page, Owner first. Each person shows status, roles with their scope, and whether they have an authenticator app.
- **Invite:** name, email, language of the emails, role and scope.
  - The invitation email is the Slice 1A one, with the company's name; the link opens `/invitation` on the tenant host.
  - The same email cannot be invited twice.
- **Person page:**
  - **Roles:** give and remove.
  - **Status:** suspend, lock, deactivate (left the company) or reactivate, each with a reason. Every session of the person ends.
  - **Security:** reset the authenticator app (the person enrols again at the next sign-in); sign the person out everywhere.
  - **Ownership:** only the Owner can transfer it, only to an active Administrator who has an authenticator app, and only after ticking a confirmation. The former Owner becomes an Administrator.
- **Protections** (also domain rules):
  - nobody acts on the Owner;
  - nobody uses these actions on their own account;
  - an invited person who never chose a password cannot be reactivated.

### Second factor and step-up

- An Administrator, like the Owner, must enrol an authenticator app at the first sign-in (D-O2). Other staff may sign in with a password alone. "Require it for all staff" is a tenant setting in Slice 1E.
- **Step-up:**
  - **Needed for:** status changes, second-factor resets, ending sessions, giving and removing roles, changing a role's permissions, deactivating a role, ownership transfer, and new recovery codes.
  - **Window:** these actions need an authenticator code from the last 5 minutes. A sign-in with a code counts.
  - **Otherwise:** the person goes to `/staff/confirm`, enters a code, and returns to the page they were on, then repeats the action.
  - **Return address:** must be a local `/staff` address.
  - **Rate limit:** the same as the second-factor sign-in step.
- Device approval and release keep the code on their own form (Slice 1C).

### My account (`/staff/account`)

- Name, email, and whether an authenticator app is set up.
- Change the password: the current password is needed. The other sessions end.
- The sessions: when they started, IP address and browser. End any other session.
- How many recovery codes are left, and **new recovery codes** (step-up; shown once; the old ones stop working).

### Navigation and texts

- The editor menu gains Staff, Roles and My account.
- `OperationsText` grows from 642 to 772 keys in en, fr and es: the pages, the permission names and the new errors (`ops.roles.*`, `ops.staff.*`, `ops.access.permission_required`, `ops.access.step_up_required`). No "Vixly" in any of them.

### Data

Migration `StaffAndRoles`:

- `Users.IsAdministrator`;
- `UserAccounts.StatusReason` and `StatusChangedUtc`;
- new tables `Roles` (one Administrator role per company, names unique per company) and `RoleGrants` (with `RevokedUtc`, never deleted).

Every table carries `TenantId`.

## 2. Automated checks

Release builds of the three solutions: no errors, no warnings.

| Test project | Slice 1C | Slice 1D |
|---|---|---|
| Operations.Domain.Tests | 52 | 66 |
| Operations.Application.Tests | 48 | 58 |
| Operations.Infrastructure.Tests (SQL Server) | 34 | 37 |
| Operations.Api.Tests | 14 | 14 |
| Operations.Web.Tests (SQL Server) | 31 | 35 |
| Customer tests | 20 | 20 |
| Platform tests | 665 | 665 |
| Public.Web, Shared.Kernel, Shared.UI | 113 | 113 |
| Architecture.Tests | 7,901 | 8,202 |

All 9,210 pass. The global SQL script runs twice on an empty server, and over a database built with the script of the previous delivery, without errors.

What the new tests cover:

- **Over HTTP, with a real database:**
  - the Owner makes a "Website editor" role and invites Ada with it;
  - the same email again is refused;
  - Ada signs in with a password alone and enrols her computer;
  - she can open the website editor, but the staff, roles and devices pages answer "Your role does not allow this." and show nobody;
  - suspending her needs a reason; once suspended she is signed out and cannot sign in.
- **Step-up over HTTP:**
  - with a sign-in code older than 5 minutes, new recovery codes go to the confirm page;
  - a wrong code stays there;
  - a fresh code returns to My account, where the codes are shown once;
  - the confirm page never sends anywhere but `/staff`.
- **Ownership over HTTP:**
  - no transfer to an Administrator without an authenticator app;
  - the Administrator must enrol one at the first sign-in;
  - after the transfer the former Owner can no longer change the new Owner.
- **Two-warehouse matrix:**
  - a person limited to warehouse A lists only A, and can change A but not B;
  - they cannot move A to B's branch, or create a warehouse in a branch;
  - a person limited to a branch sees that branch and its warehouses;
  - a role without the permission sees nothing.
- **Access rules:**
  - grants cover their scope only; revoked grants and inactive roles give nothing;
  - granting is limited to what and where the granter holds;
  - only the Owner gives the Administrator role;
  - the role Platform receives is owner, admin or staff.
- **Domain:**
  - known permissions only, kept in catalogue order; reserved ones refused;
  - the Administrator role is locked;
  - grant scopes are checked; revoking happens once;
  - status changes need a reason and follow the allowed moves;
  - the Owner is protected; Administrators need a second factor;
  - ownership goes only to an active Administrator with an authenticator app.
- **Database:**
  - roles and grants round-trip and stay in their tenant;
  - one Administrator role and unique names per company;
  - staff are listed Owner first, with search and paging;
  - the status reason round-trips.

## 3. Where the build differs from the design note

- **Passkeys are not built yet.** The authenticator app and recovery codes are the second factor. Passkeys need the WebAuthn ceremonies on each tenant host (ADR-0012 W-6). They will be a slice of their own, with the device key pair of Slice 1C, unless the owner prefers otherwise.
- **Staff activity on the person page** (their audit entries) comes with the audit log screen in Slice 1E.
- **Language and theme on My account** come with the settings in Slice 1E.
- **Editing a person's name or email** (`ops.staff.edit`) is in the catalogue but has no page yet. The person changes it through support for now.
- **Pages** are server-rendered forms with antiforgery and the form guard, like Slices 1A and 1B. Frontend/UI designs the final look.

## 4. Deployment notes

- **Database:** run the newest global SQL script (migration `StaffAndRoles`).
- No new settings.

## 5. Questions for the owner

*Question 1 was answered on 2026-10-18 (staging as proposed, wildcard supported): see the [Slice 1E record](SLICE-1E-VERIFICATION.md), section 7.*

1. **Staging on `vixly.freedns.org`:** base domains are configuration (H-1), so staging needs only an `appsettings.Staging.json` for each host. The proposed names:
   - `vixly.freedns.org` for the public site;
   - `control.vixly.freedns.org` for the console;
   - `api.vixly.freedns.org` for the API;
   - `{slug}.vixly.freedns.org` for the tenants.

   This needs a **wildcard DNS record** (`*.vixly.freedns.org`). FreeDNS offers wildcards only on some plans and domains. If a wildcard is not possible, each staging tenant needs its own record. Which do you have?
2. **Passkeys:** a slice of their own after 1E (proposed), or before?
