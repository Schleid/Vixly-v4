# In-app help: build document

Owner update, 2026-10-27: build Customer Version 2 directly, with annotated browser-page-only screenshots, prioritizing mailbox location and usage, package announcements and new shipments. This opens the three-guide Customer priority scope ahead of the earlier stage gates without declaring other stages complete. See ADR-0023 Amendment 2 and HELP-3-V2-VERIFICATION.md.

| | |
|---|---|
| Status | **Approved for build, 2026-10-22** (owner). Built stage by stage; each stage is verified and signed before the next starts. |
| Owner | Jude (owner). Builder: Codex, following this document |
| Governed by | [Global Standard](../global/VIXLY-GLOBAL-STANDARD.md), [ADR-0023](../decisions/ADR-0023-in-app-help.md), ADR-0001, ADR-0005, ADR-0012 |
| Read with | [HELP-0 foundation design](HELP-0-FOUNDATION-DESIGN.md), [writing guide](HELP-WRITING-GUIDE.md), the stage document of the application being written |

## 1. What this builds

A help area inside each Vixly V4 application, explaining what that application is for, what it does and how its people use it. Each application's help covers its own responsibilities only.

| # | Application | Readers | Help address | Brand |
|---|---|---|---|---|
| 1 | Public site (`Vixly.Public.Web`) | Visitors and companies signing up, paying or reading the terms | `/{lang}/help` | Vixly |
| 2 | Control (`Vixly.Platform.Web`) | Vixly staff | `/help` | Vixly |
| 3 | Customer portal (`Vixly.Customer.Web`) | A company's customers | `/customer/help` | The company (`{brandName}`) |
| 4 | Operations (`Vixly.Operations.Web`) | A company's staff and partner agents | `/staff/help` | The company (`{brandName}`) |

The company's own public website, which Operations serves at `/{lang}` on the company's host, gets no help area. Its visitors are covered by the Customer help, and the staff who edit it by the Operations help.

What this does not build:
- help for Edge, the print agent or the handheld apps;
- a help site outside the applications;
- videos; annotated screenshots are added only for Public Version 2 (ADR-0023 Amendment 1);
- chat support or tickets to Vixly. Missing-package tickets are an Operations and Customer feature and are documented as one.

## 2. Stages

Owner update, 2026-10-25: build Public Version 2 with browser-page-only annotated screenshots, then start Operations Version 1 at H4a. This explicit ordering takes precedence over the original sequence below. Control and Customer completion is not implied. See ADR-0023 Amendment 1.

Build in this order. A stage starts only when the one before it is signed.

| Stage | Scope | Stage document | Verification record |
|---|---|---|---|
| H0 | Foundation: `Vixly.Shared.Help`, the checks, the `VxHelpLink` and article primitives in Shared.UI, Global Standard and AGENTS.md updates | [HELP-0-FOUNDATION-DESIGN](HELP-0-FOUNDATION-DESIGN.md) | `HELP-0-VERIFICATION.md` |
| H1 | Public help | [HELP-1-PUBLIC](HELP-1-PUBLIC.md) | `HELP-1-VERIFICATION.md` |
| H2 | Control help | [HELP-2-CONTROL](HELP-2-CONTROL.md) | `HELP-2-VERIFICATION.md` |
| H3 | Customer help | [HELP-3-CUSTOMER](HELP-3-CUSTOMER.md) | `HELP-3-VERIFICATION.md` |
| H4 | Operations help, in five parts (H4a to H4e) | [HELP-4-OPERATIONS](HELP-4-OPERATIONS.md) | `HELP-4A-VERIFICATION.md` to `HELP-4E-VERIFICATION.md` |

Each application stage (H1 to H4) does three things in one go:

1. **Wire the help into the application:** its pages, its header link, its reader rules (what the reader may see) and its tests.
2. **Write the articles in English,** from the code.
3. **Write the French and Spanish versions.** Machine drafts are allowed with `review: needs-review`.

## 3. How a stage is done

1. **Sweep.** Before writing, read the application's code for the areas in the stage document:
   - endpoints, pages and components;
   - use cases and their permission checks;
   - the `.resx` English labels;
   - the plane's slice design notes in `docs/<plane>/`.

   Record anything the stage document gets wrong in the verification record under *Corrections to the stage document*. **The code wins:** write what the code does.
2. **Article list.** Start from the stage document's list. An article may be split or merged when the code calls for it; record the change.
3. **Write in English** following the [writing guide](HELP-WRITING-GUIDE.md):
   - Use the exact English label text from the `.resx` for every button, field, menu item and message you name.
   - List in `sources` the files the article was checked against.
4. **Wire.** Add the application's help pages and header link, and its reader rules, as the [foundation design](HELP-0-FOUNDATION-DESIGN.md) section 6 says for that application.
5. **Translate:**
   - French and Spanish articles keep the same slug, the same front matter (except `title`, `summary` and `review`), and the same sections and steps.
   - Labels are the French and Spanish label texts from the `.fr.resx` and `.es.resx`, never retranslated.
   - Use the [i18n glossary](../shared/i18n-glossary.md).
6. **Check:**
   - all builds pass with no warnings, and the help checks and the architecture tests pass;
   - the stage's HTTP tests pass (section 5);
   - open the help in each language in a browser and follow every article once on the development demo data ([DEVELOPMENT-DEMO-DATA](../operations/DEVELOPMENT-DEMO-DATA.md)).
7. **Record.** Write the stage's verification record (section 6) and ask the owner to sign it.

## 4. Rules that apply to every stage

- **The code is the truth.** Describe what the application does today, as the code and its English labels show it. Never describe a planned feature, never write "coming soon", never guess.
- **Do not change behaviour.** A help stage adds help and changes nothing else. A bug, a missing label or a confusing screen found while writing goes into the verification record under *Findings for the product*. Do not fix it in the help stage.
- **Only what the reader may use.** An article states its permissions and conditions in front matter and is hidden from everyone else (ADR-0023 item 4). Never write "ask your administrator for permission X" in an article that a reader without X can open. They cannot open it.
- **Tenant-facing help never says "Vixly".** In Customer and Operations articles the company is `{brandName}` (ADR-0012). Vixly appears only in Public and Control help.
- **Do not describe security mechanics.**
  - Say what the reader sees and does, for example "if you sign in from another country, you are asked for your authenticator code".
  - Never say how detection works, which headers or signals are read, or where limits sit.
  - Never document rate limits, bot-check internals or error codes.
- **No personal or real company data** in examples. Use the names from the development demo data only.
- **Follow the Global Standard** for every code file: headers, one type per file, size budgets, presenters, no literal text in markup, en/fr/es resources. Help is part of the application and passes the same gate.
- **Git:** work on the branch the dev team gives you; one pull request per stage, or per part in H4. The dev team reviews and merges.

## 5. Tests each application stage adds

- **Reachability:**
  - the help home and every article open for a reader who meets their front matter, in en, fr and es;
  - the header link opens the article written for the current page.
- **Hiding:**
  - a reader without an article's permission or condition does not see it in the list or search, and its address answers 404;
  - one test per permission group, using the roles the plane already tests with.
- **Brand** (Customer and Operations only): a rendered article shows the company's name where `{brandName}` is written, and the page never contains "Vixly".
- **Anonymous** (Public, and the signed-out Customer articles only): help pages work signed out, and are in the sitemap with their language alternates (Public).

## 6. Verification record (one per stage)

Copy this into `docs/help/HELP-<n>-VERIFICATION.md`:

```markdown
# Help stage <n> (<application>): verification

| | |
|---|---|
| Date | |
| Pull request | |
| Signed by owner | (pending) |

## Articles
| Slug | Title (en) | Permissions / conditions | Routes | fr | es |
|---|---|---|---|---|---|

## Corrections to the stage document
(what the code does differently from HELP-<n>-*.md, and what was written instead)

## Findings for the product
(bugs, unclear screens, missing or wrong labels found while writing; not fixed in this stage)

## Checks
| Check | Result |
|---|---|
| Builds, architecture tests, help checks | |
| Stage HTTP tests (section 5) | |
| Every article followed once in a browser, en | |
| fr and es pages open; `needs-review` count | |
```

## 7. Gate for each stage

- No build warnings; architecture tests and help checks pass.
- The stage's HTTP tests pass (section 5).
- Every article exists in en, fr and es.
- The verification record is complete and signed by the owner.
- `needs-review` French and Spanish articles block the release, not the stage.
