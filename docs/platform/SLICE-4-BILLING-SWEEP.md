# Slice 4: Billing - V3 behaviour sweep

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Sweep done, 2026-10-15.** Decisions in section 6 are proposals for the owner; answers go into the [design note](SLICE-4-BILLING-DESIGN.md) |
| Source | V3 `ControlPlane` (read-only): `src/Vixly.Platform.Application/UseCases/Billing` (13 use cases), `Services/Billing`, `Domain/Entities/Billing`, `Domain/Enums/Billing`, `Domain/Services/Subscriptions/SubscriptionPricingCalculator.cs`, `Infrastructure/Services/Documents`, `Vixly.Platform.API/Controllers`, console `Components/Pages/BillingOperations`, `Tenants`, `Settings` |
| Rule | Replicate behaviour, not code. Classification as in Slices 1 to 3: Active, Duplicate, Dead, Disconnected-wanted, Moved |
| Feeds | Slice 4 design note; Slice 5 (payment collection), Slice 6 (collections), Slice 7 (licensing), Slice 8 (settings, company profile) |

## 1. How the evidence was gathered

Each use case under `UseCases/Billing` was traced to the console pages, API controllers, other use cases and background services that inject it. The invoice and payment entities, the number service, the pricing calculator, the PDF renderers and the billing settings were read for their rules. The V3 snapshot has no worker host, so "nobody calls it" for a scheduled use case means nothing schedules it in V3.

## 2. Use cases (13, plus 7 outside Billing that drive it)

| V3 use case | Called from | Status | V4 outcome |
|---|---|---|---|
| CreateInvoice | Subscription assignment, capacity expansion; two console pages on the same route | Active inside; console entry broken (empty Draft, duplicate route) | Folded into one atomic "bill" use case per source (D-B5); no empty manual Draft |
| AddInvoiceLine | Subscription assignment, capacity expansion | Active inside | Part of the same atomic build; never saved on its own |
| IssueInvoice | Subscription assignment, capacity expansion | Active inside | Issue assigns the number (D-B2) and freezes the PDF (D-B10) |
| CreateCapacityExpansionInvoice | Tenant page "Manage licences"; Operations internal API | Active | Replaced by Slice 3 licence change (upgrade now, prorated); Billing invoices the "changed" message |
| GenerateRecurringInvoices | Nobody (registered only) | Disconnected-wanted | Billing invoices Slice 3's "renewed" message; one tenant per transaction |
| GetInvoices | Billing workspace | Active, no paging, no permission | `ListPlatformInvoices`, paged, filtered on the server |
| GetInvoicesByTenantId | Billing workspace, tenant page, registration | Active, no paging | Same list filtered by tenant; tenant page section |
| GetInvoiceDetails | Billing workspace, tenant page | Active | `GetPlatformInvoice` with lines, payments, credit notes, history |
| GenerateInvoicePdf | Billing workspace, tenant page | Active, English only, never stored | Stored at issue in the tenant's language (D-B10) |
| GeneratePaymentReceiptPdf | Billing workspace | Active, 80 mm thermal roll, number = payment GUID | A4 receipt with its own number (D-B11) |
| RegisterPayment | Payment routing, public registration pay | Active inside; fulfils onboarding and generates licences | `RecordPlatformPayment`; subscription activation by message to Slice 3; licences in Slice 7 |
| RegisterTenantRegularPayment | Payment routing | Active inside | Same use case; no routing by line text |
| TenantPaymentRouting | Console manual payment, card collection | Active (single entry point, routes on the line reference "CapacityExpansion") | Gone: one payment use case; the invoice knows what it bills |
| AssignTenantSubscription (Subscriptions) | Registration select plan | Active | Slice 3 `StartPlatformSubscription`; Billing invoices the "started" message |
| GetTenantRegistrationInvoice (Registration) | Public API | Active | Public site reads the registration invoice through the API (D-B13) |
| RegisterTenantRegistrationCardPayment (Registration) | Public `/pay`, anonymous | Active, **trusts the browser's amount** | Moved to Slice 5: card payments only after the provider confirms them |
| CompleteTenantRegistrationAfterVerifiedPayment | Billing workspace after a manual payment | Active | Slice 3/2 react to "invoice paid" for the registration invoice |
| CollectInvoicePayment, ProcessAutomaticInvoiceCollections | Console "Collect"; nobody | Moved | Slice 5 (provider) and Slice 6 (retries, dunning) |
| SaveBillingSettings, GetBillingSettings | Nobody | Dead, Duplicate | One settings record (D-B1, D-B4) |
| Legacy components `Billing/*` | Never routed | Dead | Not ported |

## 3. Entities and rules found

| V3 | Contents | V4 proposal |
|---|---|---|
| `Invoice` | Tenant, number (unique per tenant), currency, issued and due dates, status Draft, Issued, PartiallyPaid, Paid, Cancelled; stored subtotal, discount, tax, total, paid, balance; cancellation reason. Overdue is computed by the UI | `PlatformInvoice` (plane section 4). Overdue stays computed from the due date; status adds Void (D-B6). Language stored (plane section 7) |
| `InvoiceLine` | Description (300), type (Subscription, License, AddOn, ManualAdjustment, Discount, Tax, IntroductoryTrial), quantity (18,2), unit price, total, reference. Total not rounded in code | `PlatformInvoiceLine`: kind, description key + arguments (translated, not free English), quantity, unit price, net amount, tax rate, tax amount, period covered, source id (subscription event, charge, discount) |
| `Payment` | Invoice, amount (> 0), currency, method (Cash, BankTransfer, Card, MobileMoney, Check, Other, Zelle), paid on, reference, note, recorded by. No status, no refund | `PlatformPayment` with status (Recorded, Reversed), method, reference, received date, recorded by; receipt number (D-B11) |
| Receipt | No entity; number = payment GUID | Receipt number from its own sequence (D-B11) |
| Number service | `INV-{yyyy}-{NNNNN}` per tenant per year, found by reading the tenant's highest number with no lock; renewals `INV-REN-{period}-{tenant6}-{plan6}` | One sequence for the platform, gap-free, assigned at issue under a lock (D-B2) |
| Tax | One platform rate (fraction), label, on/off; one Tax line; rate not stored | Rates by country (and region), stored on each line (D-B1) |
| Pricing | 2 decimals, half away from zero; proration by whole days | Same rounding; proration already done by Slice 3 |
| `BillingSettings` and `PlatformBillingSetting` | Identical fields (jurisdiction, default currency, tax label, rate, enabled) | One `PlatformBillingSettings` (plane section 4) |
| `PlatformCompanyProfile` | Issuer name, legal name, address line 1, phone, email, website, logo URL | Issuer details needed by the PDF (D-B12) |

Due dates in V3 are fixed in code: 7 days for onboarding and renewals, due at once for capacity expansion, 7 or 30 days on the two manual pages.

## 4. Console screens found

| V3 page | State | V4 |
|---|---|---|
| `/billing/invoices` | Active: all invoices, filtered in the browser, search by company name only, no permission check | Invoices page, paged, filters (status, overdue, tenant, number, dates) |
| `/billing/payments` | Active: tenant lookup by phone; no Pay on a partly paid invoice; "View payment" only with exactly one payment | Payments recorded from the invoice page; a payments list |
| Tenant page invoices and "Manage licences" | Active; no price shown before the capacity invoice | Tenant page Billing section; licence changes stay on the Subscription section (previewed) |
| `/tenants/{id}/billing/create` (two pages) | Broken | Not ported (D-B5) |
| `/settings/platform-settings` billing editor | Active (tax rate as a percent) | Billing settings page (D-B4) |

## 5. Defects and risks seen in V3 (fixed by design in V4)

1. The public `/pay` endpoint records a card payment for whatever amount the browser sends, then activates the subscription and creates licences.
2. Billing permissions are defined but never checked; void and refund permissions unused.
3. Two numbering schemes; per-tenant sequences found by "read the highest and add one" with no lock.
4. Recurring invoicing never runs; when it does, one bad plan aborts the whole run, and it moves the paid-through date when the invoice is issued, not when it is paid.
5. Invoices are built in three separately saved steps, so a failure leaves half-built drafts; no idempotency on capacity invoices or manual payments.
6. The onboarding invoice uses the full monthly price for the licence line but prorated tax.
7. A partial registration payment still completes onboarding.
8. The licence count to create is read from invoice line quantities, and renewals break that convention (quantity 1).
9. Console cannot take the rest of a partial payment; receipts only for single-payment invoices.
10. English-only invoices, receipts and emails; tax rate not stored on the invoice; no tax IDs; dates in server time.
11. The invoice PDF downloads any https logo URL on the server (no allow-list, no size limit).
12. No paging anywhere.

## 6. Decisions

Proposals and owner answers are in the [design note](SLICE-4-BILLING-DESIGN.md), section 9.
