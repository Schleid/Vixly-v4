# Customer experience refinement — 2026-10-18

## Scope and design

Replaces the small floating-card customer sign-in presentation with a dedicated open layout. The pale tenant-accent panel contains a code-native illustration of the customer's online account: packages, profile and mailbox information. The sign-in area has a separate hierarchy, password recovery beside the password label, a secondary account-creation link, and contextual confirmation-link resend. On mobile the form comes first. The approved Operations staff login remains unchanged.

Published tenant logos and validated brand accents remain authoritative. New copy is isolated in CustomerExperienceText (English, French and Spanish; translations marked needs-review). Motion is limited to a brief decorative entrance and a button-arrow interaction, disabled for reduced-motion preferences. The illustration contains no invented tracking IDs, counts or delivery promises.

The package list, package details/history and pre-alert forms receive structured records, status pills, hold notices, responsive fields and compact headings. The package navigation link binds to the existing route. Shared transit color tokens cover light, explicit dark and system-dark presets; tenant overrides remain limited to existing brand accents.

Operations receiving, parcel views and inspection get scoped presentation rules for open-delivery records, view tabs, the scan lookup, customer context, photographs and audit history. Existing forms, endpoints, permissions, command keys and scanner behavior are unchanged. No backend code is included.

Changed stylesheet URLs carry a release query so an existing browser cache does not retain the old presentation.

## Verification

- Customer.Web build: zero warnings/errors.
- Customer.Web existing suite: 40 passed.
- Operations.Web existing suite: 27 passed, 42 database-dependent tests skipped.
- UI architecture checks: 474 passed.
- Actual Razor previews with synthetic data: customer sign-in, package list/detail/pre-alert, Operations receiving, inspection and parcel list/detail checked at desktop and handheld widths. Wide staff tables scroll inside their existing named containers.
- Normal sign-in with a synthetic tenant logo and the optional resend state rendered; email-to-password Tab order, original form action, and antiforgery field checked. French and Spanish error layouts fit 375px. All 14 workflow width checks measured no document-level horizontal overflow.
- Evidence is saved in the task's customer-experience-v2 visualization directory. No real credentials or customer data were used and no business form was submitted.

The separate backend baseline in the shared workspace remains an integration dependency. These checks do not establish a clean-checkout integrated deployment or validate the skipped database scenarios. This pass does not claim completion of future Operations slices or every possible permission/data state.