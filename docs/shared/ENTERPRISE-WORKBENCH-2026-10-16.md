# Enterprise component workbench — 2026-10-16

Owner: Frontend/UI. Consumer review: Control and Operations. Scope requested by the owner: current Control presentation plus reusable operational previews, with an ADR proposing the Operations UI workstream.

## Delivered

- Shared VxMetricCard (six parameters): localized labels/values, caller-provided trend visualization, and semantic delta badge. No business calculations.
- Shared VxLifecycleStepper (three parameters): labelled ordered stages and exactly one current-stage announcement.
- Additive Progress badge category; textual status labels remain consumer-owned. Canonical status palette: blue received, amber staged, purple transit, emerald completed, rose exception, with light and dark pairs.
- Compact table foundations, sticky headers, explicit sticky action-cell class, monospace numerical cells, loading busy state, bounded scrolling, comfortable handheld targets and paging.
- VxPasswordInput specializes the existing six-parameter text input, repairing the Gallery password demo without changing authentication behavior.
- Self-hosted Inter variable WOFF2 and its SIL OFL license. No font CDN dependency at runtime.
- Control: default light mode (saved dark preference preserved), compact tables and KPI/panel geometry, shared badge styling, native keyboard-operable collapsible navigation sections. Existing uncommitted menu additions are preserved but excluded from this commit's staged patch.
- Gallery route `/primitives/workbench?lang=en` (also fr/es): four metric fixtures, 150 sample rows paged by 50, branch/status/search filters, multi-select, explicit preview-only dispatch review, scan-feed/staging fixtures, lifecycle and cargo/barcode/ledger/audit detail views. Ctrl/Command+K focuses the preview tracking search. Opening a row selects its sample detail.
- [ADR-0016](../decisions/ADR-0016-operations-ui-preparation.md) is proposed, not accepted. No Operations backend stage or live workflow is opened by the preview.

## Verification

- Gallery builds with zero warnings/errors.
- Shared.UI static rendering tests: 9 passed (stage accessibility, status text/semantics, escaping, loading busy state and masked password input).
- Platform Web: 33 passed.
- Architecture: full suite passed (6,534 checks). After the final password specialization, the focused file-organization suite passed again (6,126 checks).
- Browser: 50 rows rendered, status filtering reduces the example to 30 matching rows, selecting a page reviews 30 sample items, next-page starts at VX-2026-000051, translated empty state offers clear filters, arrow keys switch detail views, Ctrl/Command+K focuses search, header/action cells compute as sticky, and all four KPI delta badges render.
- Responsive document-overflow checks at 375/768/1280/1600. Fixed a Gallery toolbar overflow found at 375. The table retains its own horizontal scrolling. Desktop sample row height is approximately 43px; touch widths use larger targets.
- French and Spanish labels and number formatting verified in the browser. Draft translations carry needs-review and require native release review.
- Light and dark preview surfaces checked. Review screenshots saved with this chat's visualization artifacts.

## Integration boundaries

Live global record search, real connectivity telemetry, authorized tenant/branch context, shipment state, carrier DIM rules, queues, scans, ledger entries and dispatch submission require Operations-owned producers/presenters. Preview selection never dispatches, invoices or persists anything. Existing Control search/telemetry placeholders are not made operational by styling. Existing Control dashboard values are outside this presentation pass.

C-1 requires owner and consumer review of the new Shared.UI APIs via pull request. Production Operations feature components remain in Operations.Web after the stage decision, never in Shared.UI or the Gallery. Shared.UI.Gallery compositions are deterministic primitive fixtures only.
