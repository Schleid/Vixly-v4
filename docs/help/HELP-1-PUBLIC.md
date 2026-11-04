# Help stage 1: Public site

| | |
|---|---|
| Status | **Approved for build, 2026-10-22** (owner). Starts when HELP-0 is signed. |
| Application | `src/Public/Vixly.Public.Web` (Vixly's own site: features, plans, sign-up, payment links, legal terms) |
| Articles | `src/Public/Vixly.Public.Web/Help/{en,fr,es}/` |
| Help address | `/{lang}/help` and `/{lang}/help/{slug}`; anonymous; in the sitemap with language alternates |
| Brand | Vixly |

## 1. Purpose of this help

Helps a logistics company:
- understand what Vixly does for it;
- choose a plan and sign up without getting stuck;
- pay its invoices from the links Vixly sends;
- read the terms.

It is the first impression of Vixly, so the sign-up articles get the most care.

## 2. Wiring

- **Pages:** `HelpHomePage` and `HelpArticlePage` under `Components/Pages`, routes with the language segment, like every Public page.
  - Static server rendering.
  - Output-cached like the home page. The search result page is not cached.
- **Navigation:**
  - add *Help* to the site header and footer (`.resx`: `nav.help`, `footer.help`);
  - the sign-up pages carry `VxHelpLink` to the sign-up article of their step (`routes` in front matter).
- **Language:**
  - the language segment chooses the catalogue;
  - the language switcher keeps the slug, which is the same in all languages.
- **Sitemap:** every article, with `hreflang` alternates, like the existing pages.
- **Rate limit:** help pages are read-only and cached. The search (`?q=`) uses the site's existing read policy, or a new named policy listed in the Public plane document (S-6).
- **Email-link pages** (`/pay/{token}`, `/cards/{token}`, `/register/verify`): their help link opens the help in the page's current language.

## 3. Sections and articles

Sections: `getting-started`, `sign-up`, `paying`, `terms-and-contact`.

| Section | Slug | Title (en) | Explains | Routes |
|---|---|---|---|---|
| getting-started | `what-vixly-does` | What Vixly does for your company | The workspace your staff use, the customer portal and the company website (with your brand), what each is for. Written from the features page and the plans; no technical words | `/{lang}`, `/{lang}/features` |
| getting-started | `choose-a-plan` | Choose a plan | Plans page: what a plan includes, limits, licences (one per device), monthly or annual, trial, promotion codes, taxes | `/{lang}/plans` |
| getting-started | `licences-and-devices` | Licences and devices | What a licence is, that each computer or tablet used by staff takes one, how adding devices works after sign-up (approved inside the company's workspace) | |
| sign-up | `sign-up-overview` | Sign up your company | The five steps at a glance (details, email, plan, payment, workspace ready), how long it takes, what to have at hand, that you can stop and continue later from the email link | `/{lang}/register` |
| sign-up | `company-details` | Enter your company details | Every field of the first step, the portal address (letters, numbers and dashes; it becomes your address), confirming the address, the owner, accepting the terms, the "I am human" check | |
| sign-up | `confirm-your-email` | Confirm your email | The email, the button on the link page, resending, correcting the email, the deadline, continuing in another browser or device | `/{lang}/register/email`, `/register/verify` |
| sign-up | `choose-your-plan-at-sign-up` | Choose your plan and licences | The plan step: plan, term, licences, promotion code, trial; accepting an offer you received | `/{lang}/register/plan` |
| sign-up | `pay-to-start` | Pay your first invoice | The first invoice, card (saving it, automatic charging and what you agree to), Zelle (the code, reporting the payment, waiting for confirmation), a cancelled payment and paying again | `/{lang}/register/pay`, `/register/payment` |
| sign-up | `after-sign-up` | When your workspace is ready | What happens after payment, the email the owner receives, the first sign-in, securing the owner account, registering the first device, where to go next | `/{lang}/register/done` |
| sign-up | `sign-up-by-invitation` | Sign up with an invitation or offer | When sign-up is by invitation only, what an offer link gives you, and contacting sales otherwise | |
| sign-up | `sign-up-questions` | Sign-up questions | Short answers: the portal address is taken; the email did not arrive; the link expired; the payment was cancelled; changing the plan later; who to contact | |
| paying | `pay-an-invoice-link` | Pay an invoice from an email | The pay link, card or Zelle, what you see after paying | `/pay/{token}` |
| paying | `save-a-card` | Save a card for automatic payments | The save-card link, what automatic charging means, how it is turned off (from Vixly support) | `/cards/{token}` |
| paying | `pay-by-zelle` | Pay by Zelle | The code, the details to report, why Vixly confirms with the bank first, how long it may take | |
| terms-and-contact | `terms-and-privacy` | Terms and privacy | The legal page, versions, documents to download, when you are asked to accept new terms | `/{lang}/legal` |
| terms-and-contact | `contact-sales` | Contact Vixly | The contact form, what happens after sending it | `/{lang}/contact` |

## 4. Sources to write from

- **Pages and flow:**
  - `Components/Pages/*`;
  - `Features/Registration/*`, `Features/Pay/*`, `Features/Plans/*`, `Features/Legal/*`, `Features/Contact/*`;
  - `RegistrationSteps.cs`.
- **Labels:** `Resources/PublicText*.resx`.
- **Behaviour on the Platform side** (verification email, plan step, payments, offers):
  - `docs/public/STAGE-2-PUBLIC-DESIGN.md`;
  - `docs/platform/SLICE-3-SUBSCRIPTIONS-DESIGN.md`, `SLICE-4-BILLING-DESIGN.md`, `SLICE-5-PAYMENTS-DESIGN.md`, `SLICE-5-ZELLE-UPDATE.md`, `SLICE-7-LICENSING-DESIGN.md`.
- **After sign-up** (the owner's first sign-in): `docs/operations/SLICE-1-STAFF-AND-SETUP-DESIGN.md`.

## 5. Notes for this stage

- Sign-up may be closed (`RegistrationOpen` off). The articles must read correctly either way:
  - say "If sign-up is open…" only where the screens differ;
  - the `sign-up-by-invitation` article covers the closed case.
- Never quote prices or plan limits in an article. They come from Platform and change; link to the plans page.
