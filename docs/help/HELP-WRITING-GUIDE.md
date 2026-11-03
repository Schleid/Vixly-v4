# Help writing guide

How to write a help article for any Vixly V4 application. Read with [HELP-PLAN](HELP-PLAN.md) and the [foundation design](HELP-0-FOUNDATION-DESIGN.md).

## 1. Who you are writing for

People at work, often busy, sometimes at a counter with a customer waiting. They want to get one thing done. They do not care how it is built.

- **Public:** a logistics company deciding whether to sign up, or one in the middle of signing up or paying.
- **Control:** Vixly staff running tenants, plans, billing and support.
- **Customer:** a company's customer sending packages abroad. Often a first-time user, and possibly reading in a second language.
- **Operations:** a company's staff (clerks, warehouse workers, cashiers, drivers, managers, owners) and partner agents at destination warehouses.

## 2. Shape of an article

1. **Opening:** one or two sentences, no heading. What it is and when you do it.
2. **`## Before you start`** (only when something must be true first): settings that must exist, what you need in hand (the customer's code, the parcel). Never permissions: a reader who can open the article has them.
3. **`## Steps`:** numbered, one action per step, in screen order. Name every button, field and menu item with its exact label in bold: **Open delivery**.
4. **`## What happens next`:** what the system does after: emails or texts sent, labels printed, statuses that change, what the customer sees.
5. **`## If something goes wrong`** (when it applies): the messages a reader may see, quoted exactly from the `.resx`, and what to do about each.
6. Related articles come from front matter. Do not list them in the body.

Not every article needs every section. A "what is this" article for a section can be a few short paragraphs.

## 3. Style

- Plain words, short sentences, active voice, second person ("you").
- **Task titles:** "Receive packages at the dock", not "Receiving" or "How to use the receiving module".
- **Labels:** copy the exact label text from the `.resx` of the article's language. If a label is wrong or unclear, write it as it is and record a finding.
- **One term for one thing.** Use the [i18n glossary](../shared/i18n-glossary.md) and the application's own label for each object: package or parcel, shipment, bundle, dispatch, invoice. Never mix synonyms.
- **Numbers and formats:** don't write amounts, currencies or units as if fixed ("$5"). They depend on the company's settings. Write "the storage fee set by {brandName}".
- **Length:** 150 to 600 words. Longer means two articles.
- **No:** jargon from the code (use case, endpoint, tenant id, entity), internal names of permissions (write the task, not `ops.receiving.receive`), "simply", "just", "easy", exclamation marks, "coming soon".

## 4. Tenant-branded help (Customer and Operations)

- The company is `{brandName}`: "{brandName} keeps your packages for 30 days" is wrong if the number is a setting; "{brandName} tells you how long it keeps packages" is right.
- Never "Vixly", never "the platform", never "the system administrator at Vixly". The reader's company is the provider.
- Settings vary between companies. Write what the reader does and what they may see, for example "If {brandName} offers text messages, you can also choose SMS". Use a condition (`text_notices`) to hide an article that only applies when a feature is on.

## 5. Translation (French and Spanish)

- Translate the meaning, keep the structure: same sections, same steps, same order.
- Labels come from `.fr.resx` and `.es.resx`, exactly as written there.
- Keep the slug, `permissions`, `conditions`, `routes`, `related`, `sources` and `updated`.
- Machine drafts carry `review: needs-review` until a native reviewer clears them.
- French: "vous". Spanish: "usted". Neutral Spanish, not regional.

## 6. Security and privacy

- **Describe behaviour, not mechanics.**
  - Say: "If you sign in from another country, you are asked for your authenticator code."
  - Never: which signals are read, thresholds, rate limits, retry counts, bot-check details or error codes.
- **Never suggest ways around a control**, even helpful ones: sharing an account, a device, a second factor or a pickup code.
- **No real names, emails, phone numbers, tracking numbers or company names** in examples. Use the demo data.

## 7. Before you call an article done

For Public Version 2, add a localized browser-page-only screenshot showing the action controls, with numbered highlights/arrows and an explanatory caption. Make the image open at full size and provide localized alternative text. Capture only fictional development fixtures; never include browser chrome, desktop pixels, personal browser profiles, real account data or secret links. Keep instructions complete without the image. Record image provenance and re-capture when controls change (ADR-0023 Amendment 1). Other Version 1 applications remain text-based.

- [ ] Every step was followed once in the running application with the demo data.
- [ ] Every label matches the `.resx` of its language.
- [ ] Front matter: permissions and conditions match who can actually do this in the code; `routes` are the pages the article explains.
- [ ] `sources` lists the files you checked.
- [ ] Nothing planned, nothing guessed, no "Vixly" in tenant-branded help.
