# Help stage 0 (foundation): verification

| | |
|---|---|
| Date | 2026-10-22 |
| Branch | `codex/enterprise-ui-workbench` (existing workspace branch) |
| Pull request | Pending; changes are local, alongside unrelated ongoing application work |
| Signed by owner | Jude, 2026-10-22: approved in chat once checks pass; condition met on the re-verification below |

## Delivered

- `Vixly.Shared.Help`: embedded resource loading, strict front matter parsing, immutable catalogue, permission/condition reader filtering, related and contextual route lookup, accent-insensitive search capped at 20, and Markdig rendering and publication checks.
- Markdig pinned centrally to 0.37.0. No plane, ASP.NET Core or UI dependency in the engine.
- `VxHelpLink`, `VxHelpArticle` and presentational related links, using shared theme tokens and the shared primitive stylesheet.
- Gallery fixture at `/primitives/help?lang=en` (also `fr`, `es`), with localized resources.
- Global Standard layout, dependency rules, L-11 and definition of done updated under ADR-0023. AGENTS.md already contained the approved help-plan entry and was preserved.
- All three existing solutions include the shared engine and its test project. Architecture tests check engine dependencies and all four article folders; absent/empty folders pass H0.

## Articles

No application articles or help routes are added in H0. Embedded article fixtures belong only to the engine test assembly. Public content and application wiring start in H1 after H0 passes its gate and the owner signs.

## Corrections to the stage document

No scope changes. Repository integration details:

- Shared primitive styles live in the existing `vixly-primitives.css`, which the hosts already load. New isolated styles would require host stylesheet integration before H1.
- Architecture checks use a source route inventory; application-stage Web tests must additionally supply the actual endpoint data source, as HELP-0 section 3 requires. Reader sources remain application-owned.
- Tokens are substituted only into rendered text, with HTML encoding; code spans and HTML attributes remain untouched. Portal address tokens are displayed as text, not used as link destinations.

## Findings for the product

The full architecture run failed five checks outside the help changes. These were not changed, per HELP-PLAN section 4:

| File | Finding |
|---|---|
| `src/Operations/Vixly.Operations.Web/Components/Pickup/PickupPage.razor` | 297 lines; UI-7 budget is 250 |
| `src/Operations/Vixly.Operations.Web/Components/Delivery/DeliveriesPage.razor` | 260 lines; UI-7 budget is 250 |
| `src/Operations/Vixly.Operations.Web/Components/SignIn/WorkspacePage.razor` | Missing `ui.editor.nav.dock` resource key |
| `src/Operations/Vixly.Operations.Web/Components/Customers/CustomerIncomingPackages.razor` | Missing `ui.parcels.state` resource key |
| `src/Operations/Vixly.Operations.Web/Site/OperationsPhoneDisplay.cs` | Required UTF-8 BOM is missing |

## Checks

| Check | Result |
|---|---|
| Shared.Help build | Passed; zero warnings/errors |
| Shared.UI and Gallery build | Passed; zero warnings/errors |
| Engine tests | 52 passed, including embedded resources, all required fields, unsafe Markdown, translations, permissions/conditions, signed-out readers, branding, related links and search |
| Startup cost | Passed: catalogue of 300 generated article versions under 500 ms, after pipeline/JIT warmup; synthetic short-article fixture |
| Shared.UI rendering tests | 12 passed (including 3 help primitive tests) |
| Full architecture suite | **14,591 passed; 5 failed**, all listed above; gate remains unsatisfied |
| Focused help/dependency/format checks after final edits | 95 passed |
| Stage HTTP tests | Not applicable to H0; H1–H4 own application routes and HTTP tests |
| Browser | Gallery fixture opened in en/fr/es; article hierarchy, steps, localized accessible help link and related link verified; final shared styling checked in light/dark |
| Application article walkthroughs | Not applicable to H0 |
| fr/es review | No application article drafts yet; 10 new gallery resource values carry `needs-review` |

## Gate

## Re-verification and owner approval (2026-10-22)

- Full architecture suite: **14,744 passed, zero failures**.
- Shared.Help tests: **52 passed**. Shared.UI rendering tests: **12 passed**.
- The oversized markup and missing resource keys were corrected by the ongoing application work. This continuation added the required UTF-8 BOM/CRLF formatting to `OperationsPhoneDisplay.cs`; its code and behaviour are unchanged.
- The earlier failed run above is retained as historical evidence.
- Owner explicitly approved H0 in chat with the condition "Approve H0 once checks pass". The condition is now met and H1 is open. Pull-request preparation and dev-team review remain pending; no merge or release is asserted.
