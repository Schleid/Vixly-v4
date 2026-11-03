# Help stage 0: foundation design

| | |
|---|---|
| Status | **Approved for build, 2026-10-22** (owner) |
| Governed by | [ADR-0023](../decisions/ADR-0023-in-app-help.md), [HELP-PLAN](HELP-PLAN.md), Global Standard sections 3, 5 and 6 |
| Builds | `Vixly.Shared.Help` and its tests, two Shared.UI primitives, the Global Standard and AGENTS.md updates. No application shows help yet; H1 to H4 wire it in. |

## 1. Article files

One Markdown file per article and language. The path is `Help/<lang>/<section>/<slug>.md` under the folder given by ADR-0023 for each application, embedded as a resource (`<EmbeddedResource Include="Help\**\*.md" />`).

```markdown
---
slug: receive-packages-at-the-dock
title: Receive packages at the dock
summary: Scan each carrier delivery so every package gets its label and the delivery its receipt.
section: daily-work
order: 20
permissions: [ops.receiving.receive]
conditions: []
routes: [/staff/receiving, /staff/scan]
related: [print-labels, unidentified-packages]
sources:
  - src/Operations/Vixly.Operations.Web/Endpoints/OperationsReceivingEndpoints.cs
  - src/Operations/Vixly.Operations.Web/Components/Parcels/ScanPage.razor
updated: 2026-10-30
review: ok
---

One or two sentences: what this is and when you do it.

## Before you start
...

## Steps
1. ...
```

### 1.1 Front matter fields

| Field | Required | Rule |
|---|---|---|
| `slug` | yes | Lowercase words joined by `-`, at most 60 characters. Unique within the application. **The same in all three languages.** It is the article's address and never changes once published. |
| `title` | yes | At most 70 characters. Task-first ("Receive packages at the dock"). Translated. |
| `summary` | yes | One sentence, at most 160 characters. Shown in lists and search results. Translated. |
| `section` | yes | One of the application's sections (its stage document lists them). Section names are `.resx` keys `ui.help.section.<section>`. |
| `order` | yes | A number. Sorts the articles within a section. |
| `permissions` | yes (may be empty) | **Any one** of these is enough to read the article. Codes must exist in the plane's permission catalogue. An empty list means every reader who can open the help. Public has no permissions; its list is always empty. |
| `conditions` | yes (may be empty) | **All** must be true. Names come from the application's condition list (section 6). |
| `signed_out` | Customer only | `true` for the articles a signed-out visitor may read (sign-up, confirmation, sign-in, password). Default `false`. |
| `routes` | yes (may be empty) | Application routes the article explains, as route templates (`/staff/arrivals/{dispatchId}`). The header link of those pages opens this article. A route belongs to at most one article. |
| `related` | yes (may be empty) | Slugs of up to five related articles in the same application. |
| `sources` | yes | Repository paths the article was checked against. Not shown to readers. |
| `updated` | yes | ISO date of the last content change. |
| `review` | yes | `ok`, or `needs-review` for a machine-drafted French or Spanish article. English is always `ok`. |

### 1.2 Body

- **Markdown:** CommonMark plus tables.
  - **Not allowed:** raw HTML, scripts, iframes, embedded images other than the application's help media folder (none are used in this build), headings above `##` (the page owns `#`), and links to `http:` addresses.
- **Links to other articles:** `[text](help:slug)`. The renderer turns them into the application's help address.
- **Links to the application:** relative, for example `[Receiving](/staff/receiving)`. Never absolute host names: they differ per company.
- **Tokens** (the only ones allowed):

  | Token | Where | Becomes |
  |---|---|---|
  | `{brandName}` | Customer and Operations | The company's display name |
  | `{portalAddress}` | Operations | The company's customer portal address |

  Any other `{word}` fails the check. To show a literal brace, write it as code.

## 2. `Vixly.Shared.Help`

- **References:** the BCL and Markdig (pinned version, in `Directory.Packages.props` if central package management is used). Nothing else: no ASP.NET Core, no plane, no Shared.UI.
- **Project and tests:**
  - `src/Shared/Vixly.Shared.Help/`, with its README (owner, may and must not reference);
  - `tests/Shared/Vixly.Shared.Help.Tests/`.

| Type | Responsibility |
|---|---|
| `HelpArticleSource` | Reads the embedded `Help/**/*.md` of one assembly. |
| `HelpFrontMatter` | Parses and validates the front matter. Hand-written for the fields above; no YAML library. |
| `HelpArticle` | One article in one language: front matter, rendered HTML, plain text for search. Immutable. |
| `HelpCatalogue` | All articles of one application, by language and slug. Built once at start-up and kept as a singleton. Lookups never touch the disk. |
| `HelpRenderer` | Markdig pipeline with raw HTML disabled, link rewriting (`help:` links, relative links), tables, heading anchors; token replacement happens **after** rendering, on HTML-encoded values. |
| `HelpReader` | The rules of one reader. Holds the permission codes and the conditions that are true for them; says whether an article is visible. Built per request by the application. |
| `HelpSearch` | Searches the visible articles of one language in title, summary and text. Ignores accents and case (L-9). Returns at most 20 results. In memory. |
| `HelpCheck` | The checks of ADR-0023 item 7, returning a list of problems. Used by each application's tests and by the architecture tests. |

**Start-up cost:** parsing and rendering all articles of one application must take under 500 ms. About 70 articles × 3 languages is the Operations estimate. Measure it in the tests.

## 3. The checks (`HelpCheck`)

Run by a test in each application's Web test project once its stage has articles, and by an architecture test that covers all four folders. They fail when:

1. an article is missing in a language, or a language has an article the others lack;
2. a front matter field is missing or breaks its rule (section 1.1);
3. a slug appears twice, or a `related` or `help:` slug does not exist;
4. a permission code is neither in the plane's catalogue (`OperationsPermissions`, `PlatformPermissions`) nor one of the application's reserved reader codes (Control only: `superuser`), or a condition is not in the application's list;
5. a route is not a route of the application. The Web tests compare against the application's endpoint data source;
6. a route is claimed by two articles;
7. the body contains raw HTML, a forbidden link or an unknown token;
8. a Customer or Operations article contains "Vixly" (any case);
9. a French or Spanish article has a different number of `##` sections or numbered steps than its English article (a cheap check that translations did not drop content).

## 4. Shared.UI primitives

Purely presentational, as section 5.3 requires:

- **`VxHelpLink`**: parameters `Href` and `Label`. A small "?" button with an accessible label, for page headers. Each application places it once, in its layout header.
- **`VxHelpArticle`**: parameters `Title`, `Html` (pre-rendered, trusted from the catalogue), `Related` (list of title and address) and `RelatedLabel`. It lays out one article with readable line length and the shared tokens.

Both go into `Shared.UI.Gallery` with sample content.

## 5. Pages each application adds (H1 to H4)

Each application builds the same three things, following its own layering:

1. **Help home** (`<help address>`): the sections in order, each with its visible articles (title and summary), and a search box (`?q=`).
2. **Article page** (`<help address>/<slug>`): the article, its related articles that are visible, and a link back to the help home. If the article is not visible to this reader, or does not exist, the page answers 404 with the application's normal not-found page.
3. **Header link**: `VxHelpLink` in the layout. It opens the article whose `routes` match the current route, otherwise the help home.

Pages stay thin (UI-6). A feature component with a presenter loads the reader's rules through an Application use case (section 6), then asks the catalogue. Text around the articles comes from the application's `.resx` under `ui.help.*`.

## 6. Reader rules per application

| Application | Permissions come from | Conditions |
|---|---|---|
| Public | None. Every article is public. | None |
| Control | The signed-in Vixly staff member's effective permissions (role grants, through the same source the use cases use). SuperUser sees everything. | None |
| Customer | None (customers have no permissions). | `card_payments`: the company takes cards online. `text_notices`: SMS or WhatsApp notices are offered. `credit_account`: this customer has a credit account. `insurance`: the company offers insurance products. Read from what the portal already receives from Operations; add to the existing contract only if missing (additive change). |
| Operations | The staff member's permissions from their role grants, **any warehouse** (a grant at one warehouse is enough to read the article). Use the same source as the menu (`GetOperationsMenu`). Partner agents hold only their partner role's permissions. | The company's plan features: `partner_agents`, `text_messages`, `tenant_api`, `website_theme` (from `portal_theme_customization`), `website_content` (from `homepage_content_customization`). |

**When the source of rules fails:**
- The plan service is unreachable: treat plan conditions as false, as `OperationsAccess` already does for partner agents.
- The reader's permissions cannot be loaded: show only articles with no permissions.

## 7. Updates to the repository rules (done in H0)

- **Global Standard, section 2:** add `docs/help/` and `Vixly.Shared.Help` to the layout.
- **Global Standard, section 3:**
  - add the row: `Shared.Help` may reference the BCL and Markdig only;
  - `*.Web` and `Vixly.Public.Web` may reference `Shared.Help`;
  - `*.Resources` may embed Markdown help files (still no code beyond the marker type).
- **Global Standard, section 5.5:** add **L-11**:
  - long-form help lives in Markdown files under `Help/<lang>/`, one per article and language, with the front matter of HELP-0;
  - labels stay in `.resx`;
  - the build fails on the checks of ADR-0023 item 7.
- **Global Standard, section 11:** add "the help articles describing a changed behaviour are updated in the same pull request".
- **Architecture tests:**
  - `Shared.Help` references only the BCL and Markdig;
  - the four help folders pass `HelpCheck` (folders with no articles pass).
- **AGENTS.md:** point help work to `docs/help/HELP-PLAN.md` (already added with this plan).

## 8. Tests in H0

- **Front matter:** every field rule (valid, missing, too long, bad slug, unknown section).
- **Renderer:**
  - raw HTML is escaped, not rendered;
  - `javascript:` and `http:` links are refused;
  - `help:` links rewritten;
  - tokens replaced with encoded values (`{brandName}` = `<b>` shows as text);
  - unknown token refused.
- **Catalogue:** languages complete; duplicate slugs; route claimed twice.
- **Reader:** permissions are *any*, conditions are *all*, empty lists mean everyone.
- **Search:** accents and case ignored; only visible articles; at most 20.
- **Start-up time** under 500 ms for 300 generated articles.
