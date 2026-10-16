# Slice 5: Payment collection - design note

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Approved by the owner, 2026-10-16** (section 9). Building |
| Inputs | [Sweep](SLICE-5-PAYMENTS-SWEEP.md), Slice 4 design (payments, receipts, credit notes, D-B15), Slice 3 (registration plan choice), Global Standard S-6, S-9, S-11, S-12 |
| Depends on | Slice 4 (built 2026-10-16) |

## 1. Outcome

Vixly takes card payments through Stripe without ever seeing a card number. Tenants pay an invoice or save a card on Stripe's own hosted pages, reached from a link in the invoice email or, at registration, from the public site. Stripe tells Vixly what happened through a signed webhook, and only that message records the payment, issues the receipt and marks the invoice paid, so a browser can never claim a payment. Tenants who agree are charged automatically on the due date; billing staff can charge a saved card from the invoice page and refund to the card against an approved credit note. Payment at registration moves the registration on to account setup. Retries, reminders and suspension stay with Slice 6.

## 2. Data (schema `platform`)

| Entity | Table | Notes |
|---|---|---|
| `PlatformMerchantAccount` | `MerchantAccounts` | One row. Provider (Stripe; Simulated in Development only), mode (Test or Live, read from the configured key and checked against the environment), publishable key, accepted currencies, card payments on or off, last connection check and its result, row version. **No secrets** (D-P8) |
| `PlatformTenantCardProfile` | `TenantCardProfiles` | One per tenant: provider customer id; automatic charging on or off with the consent (who, which email, when, consent text version) (D-P3, D-P4) |
| `PlatformSavedCard` | `SavedCards` | Tenant, provider payment-method id (unique), brand, last four, expiry month and year, default, status (Active, Removed), how it was added, removed by and when |
| `PlatformCardTransaction` | `CardTransactions` | Tenant, invoice, kind (Charge, Refund), origin (Pay link, Registration, Staff charge, Automatic, Refund, Excess refund), amount and currency, status (Created, Pending, Requires action, Succeeded, Failed, Cancelled), provider references (checkout, payment, refund), saved card, credit note (refunds), Slice 4 payment once applied, failure code (never provider text), idempotency key (unique), created by, row version. **At most one open charge per invoice** (filtered unique index) |
| `PlatformPaymentLink` | `PaymentLinks` | Kind (Pay invoice, Save card), tenant, invoice, token hash (unique), expiry, used at. Tokens are random, only the hash is stored |
| `PlatformProviderEvent` | `ProviderEvents` | Webhook inbox: provider event id (unique), type, object id, received, processed, outcome |
| Invoices (changed) | `Invoices` | + refunded to card (total); refund owed now subtracts it |
| Registrations (changed) | `TenantRegistrations` | Pending payment moves to Pending account setup when the registration invoice is paid |

Amounts are sent to Stripe in the currency's minor units, computed by the server from the invoice balance; no amount is ever read from a browser.

## 3. Rules

### Cards and consent (D-P2, D-P3, D-P4)

- Cards are entered only on Stripe Checkout (payment or setup mode). Vixly stores the payment-method id, brand, last four and expiry.
- A saved card is added by the tenant through a "save card" link (emailed by billing staff, valid 7 days, single use) or by ticking "save this card" when paying. The newest card becomes the default.
- Automatic charging needs the tenant's own consent, given on the public page with the consent text shown; staff can turn it off but never on.
- Staff remove a card with step-up; it is detached at Stripe.

### Charging (D-P4, D-P5, D-P13)

- **Pay link:** each invoice email carries a "Pay by card" link while card payments are on and the currency is accepted. The link opens Checkout for the invoice balance at that moment.
- **Staff charge:** from the invoice page, the saved default card, for the balance, with step-up.
- **Automatic:** once, on or after the due date, for tenants with consent and a valid default card. No retries here: a failure publishes "card payment failed" for Slice 6 and emails the billing contact with a pay link.
- Only one open charge per invoice; the idempotency key is the transaction id, so a repeated request or a host race cannot charge twice.
- The synchronous answer from Stripe only moves the transaction to Pending or Requires action. The payment is recorded when the signed webhook confirms it: a Card payment with the Stripe payment id as reference, a receipt, "invoice paid", and for a registration invoice the registration moves on.
- If the invoice was settled meanwhile, the part above the balance is refunded to the card automatically.
- A clock asks Stripe about transactions still open after 30 minutes, so nothing stays stuck; an unknown result is never treated as a failure.
- Requires action (3-D Secure): the billing contact gets the pay link; nothing is retried silently.

### Refunds and disputes (D-P6, D-P15)

- A card refund is sent by billing staff with step-up, against a credit note the sales manager approved, up to the smaller of the credit and what was charged by card on that invoice and not yet refunded.
- When Stripe confirms, the invoice's refund owed drops and the history shows it.
- A dispute opened by the bank is shown on the invoice and in the transaction list; a lost dispute reverses the payment (Slice 4 reversal).

### Webhook (D-P5, S-6)

- `POST /payments/webhook` on Platform.Api: anonymous, signature checked with the webhook secret, 64 KB body cap, rate-limited, deduplicated by event id. The event is stored and handled through the outbox, so a failure is retried.

### Secrets and modes (D-P8, D-P9)

- `PlatformPayments:Stripe:SecretKey` and `PlatformPayments:Stripe:WebhookSecret` come from environment variables (S-11) and are never logged or shown.
- A test key outside Development and Staging, or a live key in Development, keeps card payments off and says why on the Payments settings page.

## 4. Permissions

`platform.payments.configure` (step-up: merchant account); `platform.billing.charge_cards` (step-up: charge a saved card, send a save-card link, remove a card, turn automatic charging off); `platform.billing.refund_cards` (step-up). The Billing clerk gets charge cards; the CFO gets configure and refund; the administrator holds all.

## 5. Use cases

Get and update the merchant account, check the connection; tenant cards and consent (list, send save-card link, remove, turn automatic charging off); charge an invoice's saved card; refund to card; list card transactions; receive a provider event (webhook) and apply it (handler); the payments clock (automatic charges, stale transactions, expired links); public: open a pay link, start checkout from a pay link, start card setup, registration invoice and registration checkout.

## 6. Platform.Api additions (anonymous, S-6)

| Endpoint | Policy |
|---|---|
| `POST /payments/webhook` | Signature required; 64 KB; 600 per minute |
| `GET /payment-links/{token}` | Invoice summary for the public page (number, tenant, balance, currency, due date, PDF available); 60 per hour per IP |
| `POST /payment-links/{token}/checkout` | Body: save card, automatic charging consent. Returns the Checkout URL; 20 per hour per IP |
| `GET /registration/invoice` | Registration access token; the registration invoice summary |
| `POST /registration/checkout` | Registration access token; returns the Checkout URL |

The public site (Public plane) shows these pages; until it exists the API can be used directly. In Development the Simulated provider serves a small checkout page on the API so the whole flow can be tried without Stripe.

## 7. Console screens

Payments settings (provider, mode, publishable key, currencies, on or off, check connection); invoice page gains Card payments (transactions, charge saved card, refund to card on an issued credit note); tenant Billing section gains saved cards, automatic charging status, send save-card link, remove card; Card transactions list (status, origin, tenant, date).

## 8. Done when (in addition to the slice gate)

- No path records a card payment except a verified provider event; replayed events and racing hosts change nothing twice.
- One open charge per invoice; amounts always from the server; excess refunded.
- Automatic charging only with consent; one attempt; failure handed to Slice 6.
- Refunds only against approved credit notes, never above what was charged.
- Registration paid by card moves to account setup; no setup link in any API answer.
- Secrets only from configuration; no card data or provider error text in logs, emails or screens; texts in en, fr and es.

## 9. Owner decisions

Settled by the owner on 2026-10-16: D-P1 Stripe, D-P4 opt-in, D-P8 environment variables; all others "as proposed" in the [sweep](SLICE-5-PAYMENTS-SWEEP.md), section 6.

## 10. Build notes

Decisions taken while building; none changes an owner decision.

- **One webhook path for every provider:** `POST /payments/webhook`. The provider in use is set on the server, so the path does not name it. Stripe signs with the `Stripe-Signature` header.
- **Stripe events used:** `checkout.session.completed`, `checkout.session.expired`, `setup_intent.succeeded`, `payment_intent.succeeded`, `payment_intent.payment_failed`, `payment_intent.requires_action`, `refund.created`, `refund.updated`, `charge.dispute.created`, `charge.dispute.closed`. Others are stored and ignored.
- **Saving a card while paying:** Stripe names the card on the payment event, not on checkout completion, so the card is saved when the payment succeeds. Saving it twice changes nothing.
- **Simulated provider:** used in Development unless `PlatformPayments:Provider` says otherwise. Its checkout page is `/dev/simulated-checkout/{id}` on Platform.Api and exists only in Development with the simulated provider. Its events are signed and go through the same webhook.
- **Modes:** Production must use live keys (`sk_live_`/`rk_live_`); every other environment must use test keys. A mismatch makes card payments unavailable (`mode_mismatch`) instead of charging in the wrong mode.
- **Card-problem emails** (failed charge, authentication needed) carry a new pay link. Invoice emails carry a pay link when card payments are ready for the invoice's currency.
- **A pay link that cannot be saved** fails the outbox message so that it is retried, rather than emailing a link that does not exist.
- **International email addresses** (for example `lucía@...`) are sent with SMTPUTF8. Before this fix the mail failed on every attempt (found in the end-to-end run; test added).
