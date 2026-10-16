# Slice 5: Payment collection - verification record

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Date | 2026-10-16 |
| Built on | `8c3ccca` plus Slice 4 (not committed: the dev team owns git) |
| Design | [Design note](SLICE-5-PAYMENTS-DESIGN.md) (build notes in section 10), [sweep](SLICE-5-PAYMENTS-SWEEP.md) |
| Status | **Built and verified with the simulated provider; waiting for owner sign-off and a Stripe test-mode run** |

## 1. Automated checks

Release build of `Vixly.Platform.slnx`: no errors, no warnings.

| Test project | Before (Slice 4) | After |
|---|---|---|
| Platform.Domain.Tests | 248 | 258 |
| Platform.Application.Tests | 169 | 180 |
| Platform.Infrastructure.Tests (with SQL Server) | 41 | 48 |
| Platform.Api.Tests | 8 | 8 |
| Platform.Web.Tests | 31 | 31 |
| Architecture.Tests | 4,067 | 4,494 |
| Shared.Kernel.Tests | 81 | 81 |

All pass.

What the new tests cover:
- Merchant settings: key prefix must match the mode; enabling needs currencies and a key.
- Minor units, including zero-decimal currencies.
- Saved cards: expiry and the default card.
- Transaction states: a success is recorded once; a cancelled charge cannot succeed.
- Invoices: card refunds and refund owed; a lost dispute reverses the payment.
- Pay link: checkout for the server balance; only the webhook records the payment, once, with a receipt.
- Saving a card while paying, from the payment event.
- Forged webhooks refused.
- An abandoned checkout gives way to a new one, never two open charges.
- Not ready, wrong currency or wrong mode refused.
- Staff charge: needs a valid card; a failure reaches the tenant.
- Refunds limited to an approved credit note and to what was charged.
- Automatic charging only with consent; a provider timeout is settled by the clock's lookup.
- Registration paid by card moves to account setup.
- Database: mapping, one open charge per invoice (filtered unique index), event deduplication, links and queries.
- Mail to a non-ASCII address.

## 2. End-to-end run (console and API, simulated provider, fresh Development database)

Fresh database from the global script, first SuperUser (Jude), a sales manager (Ada), `--seed-demo-tenants`, Platform.Api running beside the console.

| # | Check | Result |
|---|---|---|
| 1 | Payments settings | Provider and mode shown, secrets "set by the server administrator"; currencies `usd, eur, CAD` saved as CAD, EUR, USD with step-up; "Check the connection": Connected, card payments Ready |
| 2 | Forged webhook | `400`, nothing stored |
| 3 | Save-card link (tenant page) | Emailed in the tenant's language; API link summary; simulated checkout; card VISA •••• 4242 saved as default; automatic charging on with the consent email and time shown |
| 4 | Automatic charge | The clock charged the due invoice; payment recorded from the webhook; invoice Paid |
| 5 | Pay link from the invoice email | "Email again" sends the invoice with a pay link; link summary shows the server balance; checkout paid; receipt recorded with the provider reference; invoice Paid; card saved as asked |
| 6 | Credit note and refund to the card | Credit note requested by Jude, approved by Ada; refund of 10.00 sent with step-up; "refunded to the card" in the history; credited 10.00, balance 0.00 |
| 7 | Staff charge | Saved card charged from the invoice page with step-up; "appears once the provider confirms it"; invoice Paid |
| 8 | Card payments list | All charges and the refund with tenant, invoice, origin (Automatic, Pay link, Staff charge, Refund), status, reference and who |
| 9 | Navigation | "Card payments" under Commercial, "Payments settings" under System |
| 10 | Outbox | No message left failing; emails to `lucía@...` delivered |

## 3. Defects found and fixed during verification

- **Invoice page error "A second operation was started on this context instance".** The card payments panel reloaded on every re-render of the page. It now reloads only when the invoice changes.
- **Emails to addresses with accents failed on every attempt.** The demo tenant's contact `lucía@atlas-messagerie.test` was refused by the mail client. The outbox message kept failing and held up later messages for that tenant. Mail is now sent with SMTPUTF8 (test added).
- **A card saved while paying by link was not kept.** The card is named only on the payment event, which did not save it. It now does, once (test added).
- **A pay link that failed to save could still be emailed.** The message now fails and is retried instead.

## 4. For the dev team

- **Database:** run the updated global script (payments tables, invoice refunded total).
- **Stripe secrets** as environment variables on Platform.Api and the console: `PlatformPayments__Stripe__SecretKey` and `PlatformPayments__Stripe__WebhookSecret`. Test keys (`sk_test_`) everywhere except Production, which must use live keys. Enter the publishable key in Payments settings.
- **Stripe webhook:** point it at `https://<api host>/payments/webhook` with the events listed in the design note, section 10.
- **Public site:** the emailed links open `/pay/{token}`, `/cards/{token}` and `/register/payment` on the public site. Those pages call `GET /payment-links/{token}`, `POST /payment-links/{token}/checkout`, `GET /registration/invoice` and `POST /registration/checkout`, then redirect to the returned checkout URL.
- **Outbox:** the dispatcher does not log a handler's exception, and one message that keeps failing holds up later messages for the same tenant. Worth a log line and an alert.

## 5. Open items

- Owner sign-off of Slice 5.
- One run against Stripe test mode with real test keys (not possible from here).
- Retries and dunning after a failed charge: Slice 6.
