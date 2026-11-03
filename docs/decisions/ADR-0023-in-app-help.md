# ADR-0023: In-app help for each application

- Status: **Accepted** (owner, 2026-10-22)
- Date: 2026-10-22
- Deciders: Jude (owner)
- Affects: Public, Platform (Control), Customer, Operations; `Shared.UI`; a new `Vixly.Shared.Help` project; Global Standard sections 2, 3, 5.5 and 11

## Context

Vixly V4 has four applications with different readers: visitors and new companies on the Public site, Vixly staff in Control, a company's customers in the Customer portal, and a company's staff in Operations. None of them has help pages today. The only help is about 110 inline hints in the resources.

The owner asked for a separate help area in each application, explaining its purpose, its features and how people use it, and focused on that application only. The help is built in this order: Public, Control, Customer, Operations. Its owner answers (2026-10-22):

- The help lives **inside each application**, written as Markdown files kept in the repository.
- It comes in **English, French and Spanish**, like the applications.
- Customer and Operations help is **tenant-branded**: it uses the company's name and never says "Vixly" (ADR-0012 W-1).

Two existing rules shape how this is built. Section 5.5 (L-2) puts user-facing text in `.resx` resources, which suit labels but not articles of several paragraphs. And the owner's standing rule, "what I am not allowed, I do not need to see", applies to help as much as to menus.

## Decision

1. **Articles are Markdown files, one per article and language,** embedded in the assembly that owns the application's text:

   | Application | Folder |
   |---|---|
   | Public | `src/Public/Vixly.Public.Web/Help/{en,fr,es}/` |
   | Control | `src/Platform/Vixly.Platform.Resources/Help/{en,fr,es}/` |
   | Customer | `src/Customer/Vixly.Customer.Resources/Help/{en,fr,es}/` |
   | Operations | `src/Operations/Vixly.Operations.Resources/Help/{en,fr,es}/` |

   This extends L-2: labels stay in `.resx`; long-form help lives in these Markdown files. The page chrome around the articles (titles, search box, "related", "not found") is ordinary `.resx` text.

2. **Each article starts with front matter** that says what it is and who may read it: a stable slug shared by its three languages, its title, a one-line summary, its section and order, the permissions and conditions needed to read it, the application routes it explains, related articles, and the code it was written from. The fields are defined in [HELP-0-FOUNDATION-DESIGN](../help/HELP-0-FOUNDATION-DESIGN.md).

3. **A new project, `Vixly.Shared.Help`,** loads, checks, renders and searches the articles. It references the BCL and **Markdig** only, and nothing of any plane.
   - Markdown is rendered with raw HTML **disabled**. Links may only be relative or `https`, and images may only come from the application's own help media folder.
   - Articles are parsed once at start-up into an immutable catalogue per language.

4. **Help shows readers only what they may use.**
   - An article whose permissions or conditions the reader does not meet is not listed, not found by search, and answers *not found* if its address is typed.
   - An article never contains instructions for a permission its readers may lack; that becomes a separate article.
   - Hiding help is not authorization (S-1). The use cases still check every action.

5. **Tenant-facing help is branded.** Customer and Operations articles write the company's name as `{brandName}`, and the build fails on the word "Vixly" in them. Public and Control help is Vixly's own and names Vixly normally.

6. **Help describes what is built, written from the code.** Each article lists the source files it was checked against. A pull request that changes a documented behaviour updates its articles (Definition of Done). Planned features are never described.

7. **The build checks the articles** with the same strictness as L-10. It fails when:
   - an article is missing in a language, or its front matter is invalid;
   - a slug is duplicated, or a related slug, permission code, condition or route does not exist;
   - an article contains raw HTML, or a tenant-facing article contains "Vixly".

   Machine-drafted French and Spanish articles carry `review: needs-review`. They pass the build and fail the release gate until a native reviewer clears them, as for `.resx`.

## Consequences

- Each application gains `/help` pages:
  - Public: `/{lang}/help`, anonymous and in the sitemap.
  - Control: `/help`, signed in.
  - Customer: `/customer/help`. The sign-up and sign-in articles can be read signed out; the rest are signed in.
  - Operations: `/staff/help`, signed in.
- Each application has a help link in its page header. The link opens the article written for the current page, or the help home when there is none.
- Global Standard updates:
  - Section 2 lists `docs/help/` and `Vixly.Shared.Help`.
  - Section 3 adds the dependency row.
  - Section 5.5 gains rule L-11 (help articles).
  - Section 11 adds "the help articles of the changed behaviour are updated".
- The Public help pages are anonymous and are listed in the Public plane document with their rate-limit policy (S-6).
- Version 1 is text-based. Public Version 2 adds screenshots under Amendment 1 below; other applications remain text-based until separately authorized.
- The work is planned in [docs/help/HELP-PLAN.md](../help/HELP-PLAN.md) and built stage by stage, each with its verification record.

## Amendment 1: Public Version 2 and Operations Version 1

Accepted by the owner on 2026-10-25, following the earlier decision to retain text-only Version 1 and add annotated screenshots in a later version.

- Public Version 2 supplements the existing instructions with actual browser-page captures and numbered highlights/arrows directing readers to the relevant controls. Never capture the desktop, other windows, browser tabs or address bar.
- Capture the real application components using fictional development fixtures. Never publish real customer information, personal browser data, account tokens or card details. Do not invent or redraw application controls.
- Images are localized with the corresponding en/fr/es screen. Text instructions remain complete without images; each image has meaningful localized alternative text. Serve images only from the application's help-media folder, retaining existing Markdown safety rules.
- Screenshot provenance records the page, language, fixture and marked controls. Re-capture when the documented UI changes. Images may not substitute for validating the workflow itself.
- The owner explicitly orders Public Version 2 followed by starting Operations Version 1 (H4a). This opens Operations without claiming completion or sign-off of unfinished Control or Customer help. Operations Version 1 remains text-based, company-branded and permission-filtered.

## Amendment 2: Customer Version 2 priority guides

Accepted by the owner on 2026-10-27. Build Customer Version 2 directly, prioritizing finding and using the mailbox address, announcing a package, and creating a shipment. This explicit instruction opens the Customer work ahead of remaining stage sign-offs without declaring those stages complete.

Use annotated browser-page-only screenshots under the same privacy, fictional-fixture, localization and provenance rules as Public Version 2. Customer media stays under `/customer/help/media/`; complete text instructions remain available without images. Company branding and verified customer-session checks apply. The three-guide priority release does not claim that all Customer help topics are complete.
