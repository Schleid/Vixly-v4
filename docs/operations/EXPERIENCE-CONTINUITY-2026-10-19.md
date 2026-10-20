# Operations and Customer experience continuity — 2026-10-19

Scope: the 63 Operations and 20 Customer page components currently present in the working tree. This covers the existing tenant site and access flows, staff workspace, receiving and inspection, parcel and shipment records, holds, inventory counts, pricing, support, administration, customer mailbox/packages, requests, account and recovery screens. Future slices are outside this inventory.

## Presentation changes

- Operations keeps its persistent workbench navigation and permission-filtered task shortcuts. Compact section headings use relevant warehouse, parcel, people, document, security or configuration drawings. Home pairs the real company/checklist with illustrated task links; no invented metrics.
- Stock-count scanning and result rows, support conversations, quote records and grouped forms receive dedicated treatments. The dock scanner keeps large touch targets and existing scan/print/offline behavior while adopting semantic palette tokens.
- Customer home adds illustrated links to its existing packages, rates and tickets. Headings reflect the workflow; the tenant brand remains authoritative. Shipment instructions, recipient/options choices, rate dimensions and estimates, support status and history receive specific compositions.
- Wide customer tables have named, keyboard-focusable scroll regions. Checkbox/radio sizes, checked states and fieldset layout are corrected; narrow-screen grids collapse. Decorative drawings are hidden from assistive technology and motion respects reduced-motion preferences.
- Approved staff/customer logins and the inspection station remain the visual baseline. Existing authentication, permissions, form actions/names, antiforgery and scanner scripts are preserved. No backend logic is changed. Existing localized labels are reused.

## Review and verification

- Customer.Web and Operations.Web builds: passed, zero warnings/errors.
- Existing UI architecture source checks: 534 passed, zero failures (existing test assembly, source checks read current files).
- Server rendering: 83/83 pages passed in the local synthetic-data fixture, which enumerates the actual page component assemblies. This is presentation smoke coverage, not database, authorization, submission or scanner-device integration verification.
- Browser visual review is pending: repeated in-app browser attempts failed while attaching a webview, although the local preview responds. No desktop/mobile screenshot sign-off is claimed for this pass.
- Shared.UI API review requested: `VxWorkspaceArt(Kind)` is decorative and plane-agnostic; consumers are Operations.Web and Customer.Web (C-1). `OperationsPageHeading` and `CustomerTableViewport` stay in their owning planes.

The repository contains extensive pre-existing uncommitted backend work and new UI models. The draft UI branch depends on that working baseline; this commit is not a clean-checkout integration or deployment claim. Backend files are excluded from UI staging. Existing permission-aware workspace composition and Customer rates/tickets navigation are retained as presentation dependencies. Keep the PR draft until integration and visual review complete.