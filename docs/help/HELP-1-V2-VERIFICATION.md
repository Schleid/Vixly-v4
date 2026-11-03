# Public help Version 2: verification

| | |
|---|---|
| Date | 2026-10-25 |
| Scope | Add localized page-only screenshots with numbered highlights and arrows to the 16 Public guides |
| Authorization | Owner requested Public V2 followed by starting Operations V1; ADR-0023 Amendment 1 |
| Owner sign-off | Pending review of this version |
| Previous version | Original 48 text-only files preserved in archive/public-v1 |

## Captures

Fourteen actual Public screens in each language: overview, plans, company form, email status, email verification, plan choice, first invoice, workspace handoff, invoice payment, save-card, Zelle report, legal documents, sales contact and invitation terms. Forty-two PNGs supply the 48 translated articles; email-confirmation articles show two images.

The `build/HelpCapture` executable runs the actual Public components with fictional contract responses. The fixture has no database or external email/payment transport. A new headless browser and temporary profile capture web-page content only. No desktop, window, browser-tab or address-bar capture API is used. The PNGs contain the application's real controls with an SVG screenshot overlay for numbered outlines and arrows. No provider checkout or card-entry screen is imitated.

Capture selectors, language, page, fixture and SHA-256 are recorded in `src/Public/Vixly.Public.Web/wwwroot/help/media/v2/manifest.json`. Each article includes a localized caption, specific alternative text and a full-size image link. Fictional prices/dates are explicitly illustrative. The complete written steps remain available without images.

## Changes and findings

- Owner clarification, 2026-10-27: company-details now emphasizes that the Vixly address is permanent, distinguishes the website from a postal address, and gives available-name examples using short names or initials (Harbour Demo Logistics → hdl/harbour, Bluewater Shipping → bluewater, Andes Courier → andes). French and Spanish drafts carry the same warning and examples. Existing annotated form screenshots are unchanged; no registration behavior was modified. The updated Public help suite passed all 27 checks; these three articles contain 406–425 words each.

- Public V1's broad human-check instruction now says to complete it if shown, matching the conditional component.
- Current invoice preparation has a refresh flow in PaymentPanel. Historical V1 findings remain historical; captures follow the current components.
- The plan card currently says “Up to 1 warehouses” in English. This product wording was captured as-is and not repaired in the help stage.
- Cropped Zelle captures include padding so numbered badges/arrows remain visible. Images are responsive and can be opened at full size.

## Checks

| Check | Result |
|---|---|
| Capture fixture build | Passed, zero warnings/errors |
| Screens captured in en/fr/es | 42; correct page language and successful HTTP responses checked during capture |
| Visual inspection | English plans/company/save-card, French Zelle and Spanish plan choice inspected; arrows, badges and page-only framing checked |
| Public HTTP/content/media checks | 62 passed in the full Public Web suite, including image existence, capture hashes, localized references and anonymous PNG delivery |
| Architecture/localization checks | 15,434 passed across help, dependencies, resources, file organization and UI rules |
| Browser help pages at desktop/mobile widths | All 16 articles in en/fr/es at 1280px and 375px: 96 passed; all images loaded, localized alternatives present and no horizontal overflow. Report: build/HelpCapture/browser-verification.json |
| Native translation review | Existing 32 article drafts remain needs-review; new captions and image alternatives are included in that review |
| Real workflow walkthrough | Not certified by fixture captures; previous outstanding end-to-end checks remain recorded in HELP-1-VERIFICATION.md |

## Operations handoff

After Public V2 implementation and checks, start Operations V1 at H4a under the owner's explicit sequence. This does not imply approval of incomplete Control or Customer help. Operations V1 is text-only, company-branded and subject to its normal access/device gates.
