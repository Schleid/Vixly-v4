# Help stage 2: Control

| | |
|---|---|
| Status | **Approved for build, 2026-10-22** (owner). Starts when HELP-1 is signed. |
| Application | `src/Platform/Vixly.Platform.Web` (Vixly staff back office) |
| Articles | `src/Platform/Vixly.Platform.Resources/Help/{en,fr,es}/` |
| Help address | `/help` and `/help/{slug}`, signed in |
| Brand | Vixly |

## 1. Purpose of this help

Helps Vixly staff run the business:
- companies and their registrations;
- plans, offers, promotions and subscriptions;
- billing, payments and collections;
- licences and devices, legal terms;
- Vixly's own staff and their rights.

Each article is shown only to staff who hold one of its permissions (HELP-0 section 6). The help therefore matches what the person can do, even though Control's menu shows every item.

## 2. Wiring

- **Pages:** `HelpHomePage` and `HelpArticlePage` under `Components/Pages`, `[Authorize]`, in the main layout.
- **Navigation:**
  - *Help* goes in the System group of the menu (`MainLayout.razor`);
  - `VxHelpLink` goes in the top bar, beside the search.
- **Reader rules:** an Application use case returns the signed-in staff member's effective permission codes (or SuperUser). Reuse the existing source of effective permissions; do not compute it twice.
- **Sign-in help:** the sign-in pages are signed out. Their help (enrolment, passkeys, recovery codes, forgotten password) is reached after sign-in. The sign-in pages keep their inline hints; no help link on them.

## 3. Sections and articles

Sections: `getting-started`, `companies`, `commercial`, `billing-and-payments`, `licences-and-devices`, `legal`, `team-and-security`, `settings`.

Permissions are listed by code without the `platform.` prefix. Check each against `PlatformPermissions.cs` and use the full code in front matter.

| Section | Slug | Title (en) | Permissions (any) | Routes |
|---|---|---|---|---|
| getting-started | `control-at-a-glance` | Control at a glance | (none) | `/` |
| getting-started | `sign-in-and-second-factor` | Sign in and keep your account safe | (none) | `/account` |
| getting-started | `passkeys-and-recovery-codes` | Passkeys and recovery codes | (none) | |
| companies | `find-a-company` | Find a company | tenants.view | `/tenants` |
| companies | `company-page` | Read a company's page | tenants.view | `/tenants/{id}` |
| companies | `edit-company-details` | Edit contacts, account manager, tax details and notes | tenants.edit, tenants.add_notes | |
| companies | `change-company-status` | Activate, suspend, reactivate or close a company | tenants.activate, tenants.suspend, tenants.reactivate, tenants.deactivate | |
| companies | `registrations` | Follow registrations | tenants.view | `/tenants/registrations`, `/tenants/registrations/{id}` |
| companies | `company-change-requests` | Review company change requests | tenants.review_changes | `/tenants/changes`, `/tenants/changes/{id}` |
| commercial | `plans` | Create and publish plans | plans.manage, plans.publish | `/plans`, `/plans/new`, `/plans/{id}` |
| commercial | `subscriptions` | Manage a company's subscription | subscriptions.view, subscriptions.manage | `/subscriptions` |
| commercial | `trials-limits-and-charges` | Trials, limit overrides and one-time charges | subscriptions.manage, subscriptions.override_limits, subscriptions.charges | |
| commercial | `discounts-and-promotion-codes` | Discounts and promotion codes | discounts.grant, promotions.manage | `/promotions`, `/promotions/new`, `/promotions/{id}` |
| commercial | `offers` | Send an offer link | offers.manage | `/offers`, `/offers/new`, `/offers/{id}` |
| commercial | `approvals` | Approve requests | discounts.approve, plans.publish, promotions.approve, billing.approve_credits | `/approvals` |
| commercial | `leads` | Follow up contact requests | leads.view, leads.manage | `/leads`, `/leads/{id}` |
| billing-and-payments | `invoices` | Invoices: read, resend, credit and void | billing.view | `/invoices`, `/invoices/{id}` |
| billing-and-payments | `record-payments` | Record or reverse a payment | billing.record_payments | |
| billing-and-payments | `card-payments` | Charge, refund and save cards | billing.charge_cards, billing.refund_cards | `/payments/transactions` |
| billing-and-payments | `confirm-zelle` | Confirm Zelle payments | billing.record_payments | `/payments/zelle`, `/payments/zelle/{id}` |
| billing-and-payments | `collections` | Collections: reminders, extensions and blocking | collections.view, collections.manage, collections.extend | `/collections`, `/collections/{id}` |
| licences-and-devices | `licences-and-devices` | Licences and devices of a company | licensing.view | `/devices/{id}` |
| licences-and-devices | `releases-and-revoking` | Decide releases and revoke devices | licensing.approve_release, licensing.manage | |
| licences-and-devices | `owner-recovery` | Help a company owner who lost access | licensing.manage | |
| legal | `legal-terms` | Prepare and publish terms | legal.manage, legal.publish | `/legal`, `/legal/{id}` |
| legal | `acceptance-register` | Follow acceptances and send reminders | legal.view, legal.remind, legal.record_acceptance | `/legal/register` |
| team-and-security | `invite-staff` | Invite a staff member | users.invite, users.create | `/users`, `/users/invite` |
| team-and-security | `manage-a-staff-account` | Lock, suspend or close a staff account | users.lock, users.suspend, users.reactivate, users.deactivate, users.reset_second_factor, users.end_sessions, users.send_password_reset | `/users/{id}` |
| team-and-security | `roles-and-permissions` | Roles and permissions | roles.view, roles.create, roles.edit_permissions, roles.assign | `/roles`, `/roles/new`, `/roles/{id}`, `/roles/permissions/{code}` |
| team-and-security | `departments` | Departments | departments.view | `/departments`, `/departments/new`, `/departments/{id}` |
| team-and-security | `audit-and-activity` | Audit log and sign-in activity | audit.view | `/audit`, `/audit/{id}`, `/activity` |
| settings | `sales-limits` | Sales limits | sales.configure | `/sales/settings` |
| settings | `billing-settings-and-tax-rates` | Billing settings and tax rates | billing.configure | `/billing/settings`, `/billing/tax-rates` |
| settings | `payment-settings` | Card and Zelle settings | payments.configure | `/payments/settings` |
| settings | `collections-settings` | Collections settings | collections.configure | `/collections/settings` |

## 4. Sources to write from

- **Pages and menu:** `Components/Pages/*`, `Components/Layout/MainLayout.razor`, `Features/*`.
- **Rules and permissions:**
  - `Vixly.Platform.Application/Security/PlatformPermissions.cs`;
  - the use cases under `Vixly.Platform.Application/UseCases/*`.
- **Labels:** `Vixly.Platform.Resources/PlatformText*.resx`, including `ui.permission.*`.
- **Behaviour:** `docs/platform/SLICE-1` to `SLICE-9` design notes (Rules, Permissions, Console screens), `docs/platform/PLATFORM-PLANE.md`, and the latest Control changes in `docs/operations/REVIEW-ITEMS-2026-10-21.md` (R-4, R-5, R-7).

## 5. Notes for this stage

- **Step-up.** Several company moves and publishing terms ask the staff member to confirm their identity again. Say so where it happens; do not explain how it works.
- **SuperUser-only actions** (banning a company, changing authority) are not in a role, so no ordinary staff member can do them. Write one short article `superuser-actions` with permission `superuser`, a reader rule for SuperUser only. Add `superuser` to Control's reader rules as a pseudo-permission, and record it in the verification.
