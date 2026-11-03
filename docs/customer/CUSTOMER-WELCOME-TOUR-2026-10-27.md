# Customer welcome tour

Date: 2026-10-27. Authorized by the owner after the Customer V2 priority guides. Built locally; not deployed.

## Behavior

New customers receive a short optional tour after completing the required home-address setup. The three steps highlight the mailbox postal address, carrier tracking field and onward destination controls. Next navigates to the actual announcement/shipment screens without submitting their business forms. Skip and Finish both remember dismissal. Escape uses the same protected Skip action. Help provides a replay link for all signed-in customers.

The modal uses localized text, native dialog semantics, explicit forward/backward Tab cycling, an announced step count and a visible target outline. Its position leaves the address and the relevant controls visible above the dialog on phones. There is no animation or dependence on local storage or a particular device. No personal data is placed in tour URLs.

## Persistence and ownership

- Operations owns `ops.Customers.WelcomeTourDismissedUtc`. The domain method is idempotent.
- The additive `CustomerProfileDto.WelcomeTourPending` defaults to false for compatibility with older Operations responses. The portal does not start an automatic tour when the server has not supplied eligibility.
- `POST /customer/tour/dismiss` requires antiforgery and forwards the opaque customer session to `POST /internal/v1/customers/me/tour/dismiss`. The use case checks the live session and resolved tenant, then changes only that customer. No posted customer ID, tenant ID or redirect destination is accepted.
- Dismissal failure returns the tour with an explanation inside its dialog. It is never presented as a successful save.
- Replay is a read-only, signed-in GET. The account's saved preference does not prevent an explicit replay.

## Migration and rollout

Migration `20261027153627_CustomerWelcomeTour` adds one nullable timestamp. It backfills existing customer rows with a dismissal timestamp, so the automatic tour is limited to subsequently created accounts. Existing customers can replay it from Help. New rows default to null.

Apply the migration before deploying the updated Operations API. Then deploy Customer.Web. No database migration or site deployment was performed during this implementation. The model/snapshot check reports no pending changes.

## Verification

- Customer portal regression and tour HTTP suite: 60 passed, no failures or skips. Includes first-address ordering, three languages, safe GET navigation, dismissal, replay, CSRF rejection, expired/staff cookies and visible failure recovery.
- Customer domain suite: 14 passed, including idempotent dismissal.
- Operations API and capture fixture builds passed with zero warnings/errors.
- The real API/database persistence and two-tenant regression is added under `tests/Operations/Vixly.Operations.Api.Tests/Tour/OperationsCustomerFlowTests.cs`. It was skipped because `VIXLY_TEST_SQL_SERVER` is not configured. Fixture/browser checks do not certify actual database persistence.
- Browser matrix: 18/18 tour-step checks passed (three steps × English/French/Spanish × 1280px desktop/375px phone). Checks include native modal presentation, target visibility above the card, focus containment, Finish, replay and Escape/Skip. Network assertions rejected any package/shipment writes. Report: `build/CustomerHelpCapture/tour-browser-verification.json`.
- Phone captures were visually inspected; the full mailbox address remains visible above the card. The final layout also stacks the home shortcuts while a phone tour is active, avoiding the long French shortcut overflowing. Browser checks use fictional data on loopback, never a personal browser profile or desktop capture.
- Corrected file-organization, UI and use-case architecture checks: 15,557 passed. The dependency, tenant-filter and help checks also passed in the initial combined run. Partial tour test classes live in Tour folders with filenames matching their type names; all four initial organization failures were corrected.
- French/Spanish tour text remains needs-review until native review. No billing, mail, warehouse or shipment data is changed by the tour.
