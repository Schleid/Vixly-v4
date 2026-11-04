# Control workbench adoption — 2026-10-16

Owner: Frontend/UI. Consumer: Control. Applies the shared enterprise UI directly to Platform.Web, beyond the Gallery preview.

## Delivered

- Home now composes four feature previews using the existing TenantListPresenter, SubscriptionListPresenter, InvoiceListPresenter and ApprovalListPresenter. Shared VxMetricCard values come from authorized totals; invoice count uses the existing overdue filter and approvals remain scoped to the viewer. No trend series, health percentages or latency are invented.
- Each preview shows at most five rows from the existing first page and links to the corresponding live directory or detail. Existing errors are localized; unavailable counts show a dash, successful empty results show zero, and loading tables announce busy state.
- Tenant, subscription and invoice lists use the same metric primitive for filtered totals, grouped responsive filter controls and monospace codes/currency. Existing paging, URL filters and actions remain in their presenters.
- Control navigation labels and theme controls are localized. The former readonly global-search placeholder is an actual GET tenant search, with Ctrl/Command+K focus. Removed simulated edge/network telemetry. Added a skip-to-workspace link.
- New en/fr/es text is complete; French and Spanish are marked needs-review for native release review.

## Data and scope

Dashboard query shape: four independent existing presenter loads, each with its own component scope. Tenant, subscription and invoice reads use page 1 of their existing paged APIs; approval loading retains the existing authorized approval-list implementation. SQL query counts inside those existing use cases are unchanged; this change does not introduce an aggregate query, schema, backend service, producer contract or financial calculation. Each metric and its preview share the same loaded state, avoiding duplicate summary/list calls.

The changes apply to Control home and the three named directories. Production shipment, dispatch and warehouse workflows remain Operations work. Other existing administration screens continue using the common shell and table styles.

## Verification

- Platform.Web builds with zero warnings/errors.
- Platform.Web tests: 37 passed, including new tests for total count versus preview size, loading-to-empty, denied data without a leaked count or rows, and functional directory-search/current-page skip-link markup.
- New resource keys: 16 matched across en/fr/es; no duplicate keys.
- New CSS uses defined tokens; blocks are balanced; scoped diff whitespace checked.
- Architecture: 6,548 passed.
- Authenticated browser visual review is not claimed for this pass; no signed-in Control browser session was available. No authentication bypass or test endpoint was added.

## Publication

Delivered on codex/enterprise-ui-workbench through draft PR #1. This is source integration, not a production deployment. Main/deployment remains unchanged until review, merge and deployment. Unrelated worktree changes are excluded from the commit.
