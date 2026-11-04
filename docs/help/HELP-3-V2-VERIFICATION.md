# Customer help Version 2 — priority guides

Date: 2026-10-27. Owner review pending. The owner explicitly requested Customer Version 2 directly and prioritized three customer difficulties (ADR-0023 Amendment 2).

## Delivered scope

| Guide | Purpose | Contextual page |
|---|---|---|
| your-mailbox-address | Find the postal address, preserve the customer code, use it at the shop checkout; distinguish it from home and website addresses | /customer/ |
| announce-a-package | Distinguish carrier, purchase store, carrier tracking number and shop order number; announce separate tracking numbers separately | /customer/packages/new |
| create-a-shipment | Choose the onward destination, recipient and packages; distinguish shipment creation from announcement, departure and payment | /customer/packages/shipments/new |

All three guides exist in English, French and Spanish. Six translated drafts remain needs-review. The English writing is short, uses actual localized field/button labels and links to the other two tasks as next steps. This is a three-guide priority release, not completion of every topic in HELP-3-CUSTOMER.

## Help engine and privacy

- `/customer/help` supports search. The portal header provides a contextual question-mark link and a visible Help link. The selected page opens its matching published guide.
- Signed-out help home asks the reader to sign in. The three account guides require a session verified through Operations, not merely a cookie. Expired sessions are rejected; service failure fails closed. Staff cookies grant no access.
- Branding comes from the resolved company. Token replacement is encoded and never stored in the shared catalogue. Article responses are not cached. No cross-plane internals or tables are accessed.
- Nine localized PNGs show actual Customer components with numbered outlines/arrows, meaningful alternative text and full-size links. Fictional examples explicitly tell customers to use their own account details.
- The loopback-only `build/CustomerHelpCapture` fixture uses fictional contract responses, ephemeral keys and an isolated headless browser. Its transport refuses writes and unknown requests. No personal browser profile, desktop, browser tabs or address bar is captured. No real customer, provider or payment service is contacted.
- Image selectors, language, page and SHA-256 are recorded in `wwwroot/help/media/v2/manifest.json`. Complete text instructions work without the images.

## Checks

- Capture fixture builds in isolated artifacts because a user-owned Customer process locks the normal output. The user's process was not stopped.
- English mailbox, Spanish announcement and French shipment captures inspected: controls, arrows, readable labels and browser-document-only framing checked.
- Customer Web suite: **53 passed**, zero failures or skips. Includes all priority guides in each language, verified-session access, expired sessions/staff cookies, unknown slugs, service failure, exact localized labels, source paths, PNG integrity and SHA-256 provenance.
- Architecture help/publication, project dependencies, file organization and UI checks: **15,516 passed**, zero failures or skips.
- Browser checks: **18/18 passed** (three guides × three languages × desktop 1280px/mobile 375px). Images loaded, alternatives and image paths matched the language, and there was no document-level horizontal overflow. Report: `build/CustomerHelpCapture/browser-verification.json`.
- The added header links initially caused mobile overflow. The help stylesheet now wraps the header and actions on narrow screens; the full browser matrix passed after the fix.
- Normal output builds encountered user-process file locks; isolated output builds completed without warnings/errors. No running user app was stopped.
- Final capture-fixture startup was smoke-checked after disabling its unused database-backed data-protection warmup; it started immediately using ephemeral keys. Capture servers were stopped after verification.

## Findings and remaining gates

- Owner follow-up, 2026-10-27: the optional first-login three-step tour is documented separately in [Customer welcome tour](../customer/CUSTOMER-WELCOME-TOUR-2026-10-27.md). This is an explicitly authorized product feature alongside the help, not a change to the business workflows described by the guides.

- The current shipment form no longer embeds new-package fields (owner change 2026-10-26). The guide follows this flow: announce separately, then select available packages on the shipment form.
- A single mailbox is the company-provided receiving address. The guides never suggest selecting a different warehouse for online shopping.
- Screenshots and read-only fixture pages do not certify actual warehouse receipt, real announcement/shipment persistence, email delivery, insurance or payment flows. Live end-to-end validation remains pending.
- Native review of the six French/Spanish drafts and new help chrome remains required before release. Owner sign-off is pending. No deployment was performed.
