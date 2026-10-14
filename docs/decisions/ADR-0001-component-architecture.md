# ADR-0001: Component architecture — self-contained feature components, thin pages

- Status: **Accepted** (owner decision, 2026-10-06): presenter class plus markup; size budgets enforced as a hard CI rule
- Date: 2026-10-06
- Deciders: Jude (owner)
- Affects: Global Standard sections 3 (rule 5), 5.3, 10 (C-3), 11; the Frontend/UI brief; every `*.Web` project

## Context

In V3, components were purely display: they took data in as parameters and raised callbacks out, and every page held all the state and logic. Measured on the V3 Operations web project (read-only inspection):

- 182 `.razor` files, 53,542 lines, **zero code-behind files**.
- `ShipmentWorkspace.razor` is 3,152 lines. About 2,880 of them are C#. It injects 26 services (23 use cases plus the authorization gate, permission service, user context and JS runtime), declares 47 fields and 36 handlers.
- Those 36 handlers are at least eight separate responsibilities: customer search, shipment list loading, shipment create and edit (with financial preview and support data), shipment details, parcel list, air and sea parcel entry (with previews), label printing, and cancel / delete.
- The "display-only" parts are not small either: `ShipmentEditor.razor` is 1,209 lines with 21 `[Parameter]`s and 5 callbacks; `ShipmentList.razor` is 684 lines with 18 parameters.

Root cause: the display-only rule forces all behavior into the one place that can reach services, which is the page. The result is a 3,000-line orchestrator, parameter-heavy children that cannot work without that parent, and no unit of behavior that can be tested or reused on its own.

## Decision

Split the UI into three tiers.

**1. Primitives (`Vixly.Shared.UI`)** stay purely presentational: buttons, inputs, grid shell, status badge, dialog, form section, empty state. No backend references. Owned by Frontend/UI. Unchanged from the Global Standard.

**2. Feature components** live in each plane's web project under `Features/<Area>/<Feature>/` and own one responsibility end to end: loading, validation, busy and error states, submit, and the use-case calls for that responsibility.

```
Features/Shipments/ShipmentEditor/
  ShipmentEditor.razor             markup                      (Frontend/UI owns)
  ShipmentEditor.razor.css         scoped styles               (Frontend/UI owns)
  ShipmentEditorPresenter.cs       state, commands, use cases  (plane team owns)
  ShipmentEditorState.cs           immutable state snapshot    (plane team owns)
  Sections/                        sub-components for form sections
```

**3. Pages** hold the route and the composition of feature components, nothing else.

### Rules

- **UI-1** A feature component has one responsibility, named as a noun phrase of the UI (`ShipmentEditor`, `ParcelList`). If its description needs "and", split it.
- **UI-2** Inputs are identities and filters (`ShipmentId`, `CustomerId`). Outputs are events that report facts (`OnSaved`, `OnCancelled`), never commands to the parent. A component never receives data it could load itself.
- **UI-3** Siblings never reach into each other. Shared state is, in order of preference: the URL (selection, tab, filters, so pages are deep-linkable); then a scoped `WorkspaceContext` carrying typed change notifications (`ShipmentChanged`) that interested components subscribe to and react to by refreshing themselves.
- **UI-4** A presenter is a plain C# class with no Blazor types. It takes its use cases and the plane's authorization-context accessor by constructor injection, exposes an immutable `State` and a `StateChanged` event, and derives from a small `PresenterBase` (in `Vixly.Shared.UI`, plane-agnostic) that provides `RunAsync` (busy flag, cancellation tied to the component's lifetime, generic error mapping per S-9). The component derives from `PresenterComponent<TPresenter>` (also in `Vixly.Shared.UI`), an `OwningComponentBase<TPresenter>`, so the presenter's DI scope lives and dies with the component.
- **UI-5** A component that calls a use case has a presenter. Trivial components (no use case, no workflow) may keep their few lines of logic in a code-behind `.razor.cs`.
- **UI-6** Pages are 80 lines or fewer, inject no use case, gate or authorization accessor, and contain no `@code` beyond reading route parameters.
- **UI-7** Size budgets, enforced in CI (the build fails): markup file 250 lines, presenter 400 lines, presenter 7 injected dependencies, component 6 `[Parameter]`s. Over budget means split. An exception needs an ADR.
- **UI-8** Feature components may use Application use cases and Shared.UI primitives. Never Infrastructure, never another plane. The architecture test also fails a type under `*.Pages` that depends on any `*.UseCases` namespace.
- **UI-9** Presenters are unit-tested with plain xUnit, no Blazor host. A bUnit smoke test per feature component is recommended, not required.

### Team ownership

`CODEOWNERS` enforces the split inside one component: `*.razor`, `*.razor.css` and `Vixly.Shared.UI/**` belong to Frontend/UI; `*Presenter.cs` and `*State.cs` belong to the plane team. The first pull request that introduces a feature component is reviewed by both.

## Worked example: ShipmentWorkspace

A sketch of how the V3 page decomposes. Handler names are from the V3 file.

| Feature component | Takes over from the V3 page |
|---|---|
| `CustomerPicker` | `HandleCustomerSearchAsync`, `HandleCustomerClearedAsync`, `ResetCustomerState` |
| `ShipmentList` | `LoadCustomerShipmentsAsync`, `FindShipment`, count and details click handlers |
| `ShipmentEditor` | `HandleNewShipmentAsync`, `HandleCreateShipmentSubmitAsync`, `HandleUpdateShipmentSubmitAsync`, `HandleEditShipmentAsync`, `HandleDestinationWarehouseChangedAsync`, `LoadCreateShipmentSupportDataAsync`, `LoadSystemPreferencesAsync` |
| `ShipmentActions` | `HandleCancelShipmentAsync` |
| `ParcelList` | `LoadShipmentParcelsAsync`, `HandleDeleteParcelAsync`, `HandleCloseParcelListAsync` |
| `AirParcelEditor`, `SeaParcelEditor` | `HandleOpenParcelEditorAsync`, `HandleParcelCreatedAsync`, `Load{Air,Sea}ParcelEntryOptionsAsync`, `Handle{Parcel,SeaParcel}PreviewChangedAsync` |
| `LabelPrinting` | `HandlePrintLabelClickAsync`, `HandlePrintShipmentCodeLabelAsync`, `HandlePrintParcelOperationalLabelAsync` |
| `FinancialPreview` | `Cancel{CreateShipment,Parcel}FinancialPreview` and the preview calls, shared by both editors |
| `ShipmentsPage` | Route and composition only |

Selected customer and selected shipment move into the URL, so none of these components needs the parent to hand it state.

## Alternatives considered

- **Keep display-only components and split the page into smaller orchestrators.** Rejected: it moves the problem and adds parameter drilling.
- **Logic in code-behind only, no presenter.** Simpler, but logic can only be tested through a Blazor host and nothing separates the two teams' files. Kept for trivial components (UI-5).
- **A global state store (Redux-style).** Rejected for now: a framework-sized cost for a problem the URL plus a small context solves. Revisit if cross-feature state grows.
- **Binding-based MVVM (`INotifyPropertyChanged`).** Rejected: Blazor has no binding layer, so an immutable state plus one event is simpler.

## Consequences

- Global Standard 3 (rule 5), 5.3, 10 (C-3) and 11 are rewritten (v1.1, done); the section 3 rule on `Shared.UI` is clarified to mean primitives.
- The Frontend/UI brief's scope shrinks to primitives, the gallery and the `PresenterBase`; feature components belong to the plane teams with Frontend/UI owning the markup.
- The gallery covers primitives only. Feature components are developed against the running app with seeded data. If the hybrid heads (V2+) need to reuse them, the `Features/` folder is lifted into a per-plane RCL; the dependency rules above make that a move, not a rewrite.
- A V3 page port follows one recipe: inventory handlers by responsibility, move each group's state and use-case calls into a presenter, move shared state to the URL or context, leave the page as composition.
