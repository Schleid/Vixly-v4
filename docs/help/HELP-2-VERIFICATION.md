# Help stage 2 (Control): verification

| | |
|---|---|
| Date | 2026-10-23 |
| Status | In progress; first content group implemented, not a completed H2 |
| Pull request | Pending; local changes on the existing workspace branch |
| Signed by owner | Pending |
| Stage opening | Owner explicitly authorized proceeding to Control on 2026-10-23. Public's outstanding walkthrough and native review remain recorded separately. |

## Articles

The first group contains 14 articles in English, French and Spanish (42 embedded files). This is not the complete HELP-2 inventory. Commercial, billing/payment, licensing, legal, staff administration and settings guides remain to be written and verified. The SuperUser responsibilities guide is included to verify the reserved reader rule.

| Slug | Title (en) | Permissions / conditions | Routes | fr | es |
|---|---|---|---|---|---|
| control-at-a-glance | Control at a glance | Signed in; no article permission | / | needs-review | needs-review |
| sign-in-and-second-factor | Sign in and keep your account safe | Signed in; no article permission | /account | needs-review | needs-review |
| passkeys-and-recovery-codes | Passkeys and recovery codes | Signed in; no article permission |  | needs-review | needs-review |
| find-a-company | Find a company | platform.tenants.view | /tenants | needs-review | needs-review |
| company-page | Read a company's page | platform.tenants.view | /tenants/{TenantId:guid} | needs-review | needs-review |
| edit-company-details | Edit company contacts and account settings | platform.tenants.edit |  | needs-review | needs-review |
| registrations | Follow registrations | platform.tenants.view | /tenants/registrations, /tenants/registrations/{RegistrationId:guid} | needs-review | needs-review |
| company-change-requests | Review company change requests | platform.tenants.review_changes | /tenants/changes, /tenants/changes/{RequestId:guid} | needs-review | needs-review |
| change-company-status | Activate a company | platform.tenants.activate |  | needs-review | needs-review |
| suspend-a-company | Suspend a company | platform.tenants.suspend |  | needs-review | needs-review |
| reactivate-a-company | Reactivate a company | platform.tenants.reactivate |  | needs-review | needs-review |
| close-a-company | Deactivate a company | platform.tenants.deactivate |  | needs-review | needs-review |
| company-notes | Add an internal company note | platform.tenants.add_notes |  | needs-review | needs-review |
| superuser-actions | SuperUser responsibilities | superuser |  | needs-review | needs-review |

## Corrections to the stage document

- Reused the existing GetMyPlatformAccess Application use case and its authoritative permission reader; no duplicate effective-permission calculation or new use case is needed.
- Routes use their actual parameter names and guid constraints rather than the illustrative id templates in HELP-2. Context matching observes guid constraints, so the change-request list is not mistaken for a company detail page.
- Company status tasks are separate articles, each with its own action permission. The original change-company-status slug now covers activation. Suspension, reactivation and deactivation have separate slugs.
- Company notes are separate from contact/account-setting edits. Tax settings require billing authority and are not included in the company-edit article.
- Legal company identity/address are locked in the normal company editor; the reviewable change-request workflow is documented instead.
- The reserved superuser reader code is accepted only by the help catalogue. SuperUser visibility comes from the current persisted access snapshot, never a role grant or a stale authority claim.
- No existing-account change-authority form was found in the current user page. The SuperUser article describes the authority choice actually available during invitation, rather than inventing a form.

## Findings for the product

- TenantLicensing still contains an untranslated Capacity Full label. Not changed in this help stage.
- Company status actions and subscription/billing status are distinct; the guides do not promise that a company move settles a billing balance or completes device setup.

## Checks

| Check | Result |
|---|---|
| Initial Control build | Passed; zero warnings and errors |
| First help HTTP/content suite | 10 passed before the lifecycle and SuperUser additions |
| Web regression / publication / presenter checks | 56 passed in the full Web suite; 15 focused checks passed after adding navigation-race protection and the menu assertion; all three language reachability checks passed again after wiring the scoped help stylesheet |
| Architecture, dependency, localization and help-publication checks | 69 passed in the focused architecture run; 14,649 file-organization and UI-budget checks passed |
| Every implemented article opens in en/fr/es over authenticated HTTP | All 42 pages passed in the final suite; exact translated labels, 150–600-word bodies and existing source paths checked |
| Browser workflow walkthrough | Pending; HTTP reachability does not substitute for following the instructions in the running application |
| Native language review | 28 article drafts and 40 chrome resource values need review; blocks release |

## Gate

H2 remains in progress and is not ready for owner sign-off. Complete the remaining Control content, its source and permission review, the checks and the demo walkthrough before requesting approval. Customer help remains unopened.

