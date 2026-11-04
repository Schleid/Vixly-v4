# Help stage 3: Customer portal

| | |
|---|---|
| Status | **Customer V2 priority build, 2026-10-27** (owner). Directly build mailbox, package announcement and shipment guides with annotated screenshots; the earlier HELP-2 gate is overridden for this scope by ADR-0023 Amendment 2. Other topics remain pending. |
| Application | `src/Customer/Vixly.Customer.Web` (a company's customers, at `{company host}/customer`) |
| Articles | `src/Customer/Vixly.Customer.Resources/Help/{en,fr,es}/` |
| Help address | `/customer/help` and `/customer/help/{slug}`. Articles with `signed_out: true` open without signing in; the rest need a signed-in customer. |
| Brand | The company: `{brandName}`. Never "Vixly". |

## 1. Purpose of this help

Helps a company's customer:
- open an account and get their mailbox address;
- announce packages and send them home;
- answer the company when a package needs something;
- pay;
- know what to do when a package is missing.

Readers are often first-time users reading in a second language, so keep sentences short and steps small.

## 2. Wiring

- **Pages:** the help home and article pages are endpoints returning components, like the rest of the portal (`Portal/*Endpoints.cs`), in the portal shell with the company's brand.
- **Signed out:**
  - the help home lists only `signed_out` articles, with a "Sign in" call to action;
  - other articles answer the sign-in page, as portal pages already do.
- **Navigation:**
  - *Help* goes in the portal menu (`CustomerShell.razor`);
  - `VxHelpLink` goes in the shell header;
  - the sign-in and sign-up pages get a "Help" link to their articles.
- **Reader rules:**
  - Conditions are `card_payments`, `text_notices`, `credit_account` and `insurance` (HELP-0 section 6).
  - Read them from what the portal already loads (brand, notice channels, invoices and credit account, insurance products).
  - If one is not available, add it to `Contracts.Operations` additively and record it.

## 3. Sections and articles

Sections: `getting-started`, `your-packages`, `shipping`, `paying`, `when-something-is-wrong`, `your-account`.

| Section | Slug | Title (en) | Signed out | Conditions | Routes |
|---|---|---|---|---|---|
| getting-started | `how-it-works` | How sending with {brandName} works | yes | | |
| getting-started | `create-your-account` | Create your account | yes | | `/customer/sign-up`, `/customer/sign-up/sent` |
| getting-started | `confirm-your-email` | Confirm your email | yes | | `/customer/verify` |
| getting-started | `sign-in-and-password` | Sign in and reset your password | yes | | `/customer/sign-in`, `/customer/forgot-password`, `/customer/reset-password` |
| getting-started | `your-mailbox-address` | Your mailbox address and customer code | | | `/customer` |
| your-packages | `announce-a-package` | Announce a package (pre-alert) | | | `/customer/packages/new` |
| your-packages | `your-packages` | Follow your packages | | | `/customer/packages` |
| your-packages | `package-details-and-photos` | Package details and photos | | | `/customer/packages/{id}` |
| your-packages | `answer-a-hold` | When a package is on hold | | | |
| your-packages | `storage-fees` | Storage fees | | | |
| shipping | `create-a-shipment` | Create a shipment | | | `/customer/packages/shipments/new` |
| shipping | `pickup-or-home-delivery` | Pickup or home delivery | | | |
| shipping | `consolidate-packages` | Send several packages together | | | |
| shipping | `insure-a-shipment` | Insure a shipment | | insurance | |
| shipping | `manage-a-shipment` | Change, add to or cancel a shipment | | | `/customer/packages/shipments/{id}`, `/customer/packages/shipments/{id}/edit` |
| shipping | `who-collects` | Name the person who collects | | | |
| shipping | `shipment-documents` | Shipment documents | | | |
| shipping | `prices-and-estimates` | Prices and estimates | | | `/customer/pricing`, `/customer/rates` |
| shipping | `track-a-shipment` | Where your shipment is | | | |
| paying | `invoices` | Your invoices | | | `/customer/invoices` |
| paying | `pay-by-card` | Pay an invoice by card | | card_payments | `/customer/invoices/card/{chargeId}` |
| paying | `other-ways-to-pay` | Other ways to pay | | | |
| paying | `credit-account` | Your credit account and statement | | credit_account | |
| when-something-is-wrong | `report-a-missing-package` | Report a missing package | | | `/customer/tickets/new` |
| when-something-is-wrong | `follow-a-report` | Follow your report | | | `/customer/tickets`, `/customer/tickets/{id}` |
| when-something-is-wrong | `damaged-or-lost` | A package arrived damaged or was lost | | insurance | |
| your-account | `profile-and-language` | Your details and language | | | `/customer/profile` |
| your-account | `notices` | Emails and text messages | | text_notices | |
| your-account | `change-your-password` | Change your password | | | |

## 4. What the articles must say clearly

These are limits of the portal, not features. Write them plainly so customers are not left searching:

- **Tracking:** there is no separate tracking page. Customers follow their packages and shipments on their own pages (`track-a-shipment`).
- **Claims:** a damaged or lost package is not claimed in the portal. The article explains the claim rules shown with the insurance terms and says to contact {brandName} (`damaged-or-lost`).
- **Paying:** card is the only way to pay online, and only when {brandName} takes cards. Other ways are arranged with {brandName} (`other-ways-to-pay`, no condition).
- **Mailbox:** packages go to the one mailbox address {brandName} gives (owner decision R-8). The article never suggests choosing another warehouse.

## 5. Sources to write from

- **Pages and flows:** `Portal/*Endpoints.cs`, `Components/Portal/*`, `Components/Pages/*`.
- **Labels:** `Vixly.Customer.Resources/CustomerText*.resx`, `CustomerExperienceText*.resx`.
- **Behaviour:**
  - `docs/operations/SLICE-2-CUSTOMERS-DESIGN.md`, the Customer side sections of every Operations slice design;
  - `docs/customer/CUSTOMER-EXPERIENCE-V2-2026-10-18.md`;
  - R-2 and R-8 in `docs/operations/REVIEW-ITEMS-2026-10-21.md`.
