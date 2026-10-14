# Vixly.Shared.UI

Razor Class Library: layout shells, list/grid virtualization wrappers, form inputs, status badges, FluentUI theme. Primitives are presentation only; also holds the plane-agnostic `PresenterBase` and `PresenterComponent<TPresenter>` (ADR-0001). Later home of `ThemeProvider`.

| | |
|---|---|
| Owner | Frontend/UI team |
| Build stage | parallel |
| May reference | Microsoft.AspNetCore.Components, FluentUI |
| Must not reference | Any plane project, any `Vixly.Contracts.*`, `HttpContext`, Application / Infrastructure namespaces (enforced by architecture test). |

Primitives take parameters and `EventCallback`s only. Primitives contain no literal text: labels, placeholders and accessibility text come from this project's own en, fr and es resources, and layouts tolerate text 40 percent longer (Global Standard 5.5, UI-10, UI-11). Feature components (which own behavior) live in each plane's `*.Web` project, not here; see Global Standard 5.3.

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md).

## What is here so far

Platform Slice 1 needed the presenter base before this project's stage opened, so the Control team added only these, for Frontend/UI review:

| File | Role |
|---|---|
| `Presenters/PresenterBase.cs` | Busy flag, cancellation tied to the component, `StateChanged`; turns a refused gate into `Denied` and any other exception into `Failed` with a logged reference the user can quote (S-9) |
| `Presenters/PresenterComponent.cs` | `OwningComponentBase<TPresenter>`: one DI scope per component; re-renders on `StateChanged`; `PrepareScope` lets a plane copy the signed-in identity into the new scope before the presenter is created |
| `Presenters/PresenterRunOutcome.cs` | `Completed`, `Denied`, `Failed`, `Cancelled` |

No primitives yet: those remain Frontend/UI work.
