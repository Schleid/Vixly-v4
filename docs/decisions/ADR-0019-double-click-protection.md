# ADR-0019: Double-click protection

- Status: **Accepted** (owner request, 2026-10-17)
- Date: 2026-10-17
- Deciders: Jude (owner)
- Affects: `Vixly.Shared.UI`, `Vixly.Shared.Hosting`, `PlatformUnitOfWork`, every plane Web project; Global Standard section 5.3 (new rule UI-12)

## Context

An impatient user clicks a button several times, or presses Enter twice, before the first request has answered. In the live console (Blazor Server) each click reaches the server as a separate event; while the first handler waits on the database, the next one starts, so the same command can run twice: two invites, two offers, two payments recorded. The busy flag on buttons does not stop this, because the disabled button reaches the browser only after the first round trip. Plain HTML forms (sign-in pages, the public site) post again on every click.

## Decision

1. **Live pages: the server drops repeats.** Every interactive component that handles a click or a submit inherits a guarded base: `PresenterComponent` (with a presenter) or `GuardedComponent` (without, such as a form section that hands its data to its parent). While one click or submit of that component is still running, further clicks and submits on it are dropped. Typing, change and keyboard events always pass, so scanners and fast typists never lose input. `IsActing` lets markup disable controls during the action.
2. **`VxButton` guards itself.** While its click handler runs it is disabled, shows the busy spinner and ignores further clicks.
3. **Static pages: the browser blocks repeats.** Every page shell that posts plain forms loads `_content/Vixly.Shared.UI/vixly-forms.js`. The first submit goes through with the clicked button's value; later submits are blocked and the submit buttons are disabled until the next page arrives, the page comes back from the Back button, or 15 seconds pass. A form that must allow repeats opts out with `data-vx-allow-repeat`. Forms keep working without JavaScript.
4. **A started commit always finishes.** When the browser abandons a request (a second click, a closed tab, a lost connection), the request's cancellation still stops reads, but never a commit in progress: `PlatformUnitOfWork` saves without the request's token. Every host treats an abandoned request as normal (`UseVixlyClientDisconnects`: logged at Debug, status 499) instead of reporting an error. Found 2026-10-17: a double click on "Verify" during sign-in cancelled the first request's save and was reported as an `OperationCanceledException`.
5. **Bot check before sending.** On a form with the bot check (Cloudflare Turnstile), `vixly-forms.js` holds the submit until the check has answered and shows a localized message instead. Platform still verifies every answer, and an answer can be used once, which also refuses a repeated post. Development uses Cloudflare's public test keys, so the check is visible and always passes locally.
6. **The server stays the authority.** Commands keep their own protection against repeats that the UI cannot see (two tabs, a replayed request): concurrency tokens, one-pending rules and unique indexes. New commands that create money movements or records customers see should also take an idempotency key; each Stage 3 slice design decides where.

## Consequences

- Global Standard 5.3 gains **UI-12**: "Every click and submit is protected against repeats (ADR-0019): interactive components inherit `PresenterComponent` or `GuardedComponent`; static shells load `vixly-forms.js`."
- `DoubleClickRulesTests` (architecture) fails when a console component with a click or submit handler does not inherit a guarded base, or a static shell does not load the script.
- bUnit tests in `Platform.Web.Tests` prove a button and a guarded form run their action once while it is still running.
- Frontend/UI keeps the disabled and busy styling of buttons; no markup change is needed for the guard itself.
