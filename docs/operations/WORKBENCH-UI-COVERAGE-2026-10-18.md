# Operations UI coverage

This completes the design/styling pass for Operations screens currently implemented in the shared workspace. The approved staff login remains intact. Operations now has one signed-in workbench, rather than a separate welcome shell and unrelated editor navigation.

## Design and coverage

| Area | Presentation coverage |
|---|---|
| Staff login | Approved tenant welcome, published logo with initials fallback, three tiles and protected-access dot; unchanged in this pass |
| Signed-in home | Real setup checklist, signed-in name and working route shortcuts; no invented KPIs, scans or queues |
| Navigation | Tenant-name identity, grouped native collapsible menus, current-page indicators, independently scrolling sidebar and permanent account/sign-out area |
| Mobile navigation | Progressive drawer, labelled controls, focus containment, Escape/backdrop dismissal, focus restoration and inert main content; without JavaScript navigation remains available in the document |
| Customers | Compact directory table, sticky identity/header cells, existing paging/search, status pills, empty state and shortcut to the existing creation form |
| Customer detail | Readable definition lists, mailbox address blocks, responsive forms and visible status |
| Staff and roles | Compact staff table, invitation shortcut, role/scope chips, empty state, permission-fieldset layout and existing sensitive-action forms |
| Locations and devices | Organized lists, location statuses, device group labels, readable actions, empty sections and responsive location/contact forms |
| Billing and audit | Named keyboard-scrollable tables, sticky headings/actions, numeric currency alignment, overdue state, filter layout and empty sections |
| Company, settings and shipping services | Shared compact forms, section dividers, readable hints and disabled/read-only controls; existing permission gates retained |
| Website editor | Same form system for brand settings, section switches, text, images, preview and publish controls; existing entitlements retained |
| Account and sessions | Shared form, details and session-list presentation; existing password/session behavior retained |
| Recovery, MFA, invitations and device setup | Compact cobalt/white access shell, readable one-time-code forms, QR/manual-key treatment, recovery-code layout and setup steps |
| Unavailable and not-found states | Dedicated tenant-neutral status surface using existing copy and response status |
| Tenant public site | Existing branded storefront, locations and tracking design retained; no change to published tenant settings |

The workbench uses shared semantic tokens, self-hosted typography and the cobalt/navy direction approved for Operations. It adds no global-search or branch-selector controls without an implemented data contract. No backend business logic, endpoints, authorization, tenancy, payment behavior or authentication policy is changed. Form actions, antiforgery, existing permission conditions and the shared submit guard remain in place.

Receiving, shipment, bundle, dispatch, delivery, operational finance and reporting screens do not exist in the current Web component inventory. Their later slices should adopt these navigation, form, table and status patterns when implemented; this record does not claim those workflows are complete.

## Verification

- Operations build succeeded with zero warnings/errors. Final Operations web tests: 15 passed, 34 database-dependent tests skipped, zero failures. Focused UI architecture checks: 454 passed.
- New workbench copy contains 19 matching, nonempty keys in English, French and Spanish. Draft French/Spanish additions carry needs-review for the native-language release gate.
- Browser review used real Razor components in an isolated local HTTPS fixture with clearly synthetic company/staff/customer/invoice data. No production sessions, database writes, invitations or payment submissions were used.
- Reviewed desktop home and customer table at 1440px; disabled settings and invoice presentation at 1280px; French mobile directory/drawer and recovery at 375px; Spanish empty-state copy at tablet width. Tables scroll within their regions without horizontal page overflow; sticky headings and identity columns verified.
- Verified mobile drawer focus, Escape dismissal, restored trigger focus and inert main content. A visibility-transition focus issue discovered during browser review was corrected. Existing disabled settings remained disabled and exposed no submit button in the read-only fixture.
- The Operations backend/project baseline is still separate uncommitted work. The styling commit explicitly includes only UI/resources/docs and does not sweep in that baseline. Clean-checkout integration and database-dependent verification remain with the plane team before release. No merge, deployment or tenant content publication was performed.

Local screenshot artifacts are provided with the review handoff. The temporary preview host is stopped after verification.
