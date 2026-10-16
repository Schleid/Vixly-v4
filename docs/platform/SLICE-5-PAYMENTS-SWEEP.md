# Slice 5: Payment collection - V3 behaviour sweep

| | |
|---|---|
| Plane | Platform (Stage 1) |
| Status | **Sweep done, 2026-10-16.** Decisions in section 6 are proposals for the owner; answers go into the design note |
| Source | V3 `ControlPlane` (read-only): `src/Vixly.Platform.Application/UseCases/PaymentCollection` (5 units) and `UseCases/MerchantServices` (4), `Domain/Entities/PaymentCollection`, `Domain/Entities/MerchantServices`, `Infrastructure/Services/PaymentCollection`, `Vixly.Platform.PIC`, `UseCases/Tenants/Registration` (card payment, completion), `Vixly.Platform.API/Controllers/Public`, console `Settings/MerchantConfiguration` and `BillingOperations/Billing/Components/Payments`, `scripts/database/*PaymentCollection*` and `*CollectionPaymentExtensions*` |
| Rule | Replicate behaviour, not code. Classification as in Slices 1 to 4: Active, Duplicate, Dead, Disconnected-wanted, Moved |
| Feeds | Slice 5 design note; Slice 4 (registration invoice endpoint, card payments, refunds owed); Slice 6 (retries, dunning, suspension); Slice 2/3 (registration completion) |

## 1. How the evidence was gathered

Each payment use case, service and console page was traced to the pages, API controllers, background services and other use cases that inject it, and the entities, EF configurations and staging SQL were read for their rules. The payment extract did not contain the `PaymentCollection` folders that the console pages use; those were read from the full V3 snapshot used for Slice 4. Neither tree has repository implementations, an Infrastructure DI file or a host, so "nobody calls it" means nothing in the snapshot does. No class implements either provider interface, in source or in the compiled PIC builds. Detailed notes with file and line evidence: `v3-payments-sweep.md`.

V3 holds two unrelated card designs. "MTPPIF" (`MerchantServices` and the `Vixly.Platform.PIC` contracts) is a **tenant-owned** connection to a tenant's own provider, for collecting from the tenant's customers; it was never wired up. `PaymentCollection` is Vixly collecting from tenants through Vixly's merchant account; it is wired up but has no provider adapter, so no card was ever charged.

## 2. Use cases (9, plus 4 outside the folder that drive it)

| V3 use case | Called from | Status | V4 outcome |
|---|---|---|---|
| CollectInvoicePayment | Console payment panel "Charge Saved Method"; automatic collection | Active code, never reachable (no provider adapter, no saved methods); amount from the invoice balance, idempotency key, attempt log | `ChargeSavedPaymentMethod`: staff (step-up) or the due-date clock; amount = balance; payment recorded only when the provider confirms (D-P4, D-P5) |
| ProcessAutomaticInvoiceCollections | Nobody (registered only) | Disconnected-wanted | One attempt on the due date by Slice 5 (D-P4); the 0/3/7-day retries go to Slice 6 (D-P13) |
| GetPlatformMerchantConfiguration, SavePlatformMerchantConfiguration | `/settings/merchant-configuration` | Active, but Active status can never be saved (no adapter); no gate in the use case | Merchant account page: provider, mode, display data; secrets in configuration (D-P1, D-P8) |
| PlatformPaymentSettingsService (Zelle get, save) | Zelle tab of the same page | Active | Folded into Slice 4 billing settings: payment instructions per language plus optional QR image (D-P10) |
| PlatformPaymentOptionsService, PlatformZellePaymentPolicy | Payment panel, RegisterPayment | Active; Duplicate of rules Slice 4 already enforces | Gone: Slice 4 `RecordPlatformPayment` rules methods; "can charge a card" is a query on merchant and saved method |
| GetPublicPlatformPaymentConfiguration (Payment collection) | Public `GET registrations/{id}/invoice` | Active; exposes the internal merchant name and a 340 KB QR on every read | Public registration payment options: "card available" and Zelle instructions in the visitor's language (D-P7) |
| RegisterTenantRegistrationCardPayment (Registration) | Public `/pay`, anonymous | Active, **trusts the browser's amount, reference and date**; works with no merchant configured | Replaced by a provider checkout created by the API, paid only on provider confirmation (D-P7) |
| CompleteTenantRegistrationAfterVerifiedPayment (Registration) | Billing workspace after a manual payment only | Active; not called after a card charge | Already replaced by Slice 4 "invoice paid"; card payments raise the same message |
| TenantPaymentRouting card leg (Billing) | CollectInvoicePayment | Active | Slice 4 payment recorded with method Card from the provider confirmation only |
| CreatePaymentProviderConnection, SavePaymentProviderConnectionSettings, ValidatePaymentProviderConnection (Merchant services) | Nobody; not in DI; no tables | Dead in Platform (tenant-owned design) | Not ported; an Operations backlog item if tenants ever take card payments (D-P1) |
| CollectCreditCardPayment (Merchant services) | Nobody; body throws `NotImplementedException`; its request has no card or token field | Dead | Not ported |
| Add, list, set default, remove a saved card | Nothing exists | Missing | New: provider-hosted card setup (D-P2, D-P3) |
| Payment collection emails (`IPaymentCollectionNotificationService`) | CollectInvoicePayment, registration completion | Active; English, company email, "Tech Crafters" fallback | Slice 4 receipt on success; Slice 5 "card failed / needs action" to the billing contact in the tenant's language (D-P14) |

## 3. Entities and rules found

| V3 | Contents | V4 proposal |
|---|---|---|
| `PlatformMerchantAccount` | Provider key, display name, configuration reference (a pointer to a secret, never the secret), status Draft, Active, Disabled; one Active (filtered unique index); last configured by. Provider key can change under existing saved methods | `PlatformMerchantAccount`: provider, mode (test, live), display name, publishable key, accepted currencies, status, row version, audit. API key and webhook secret from configuration (D-P8). Changing provider or account retires saved methods (D-P1) |
| `PlatformPaymentSettings` | Zelle singleton: on/off, recipient name, email or phone, instructions, QR data URI, revision | No entity: Zelle text in Slice 4 billing settings per language, QR as a stored image (D-P10) |
| `TenantBillingPaymentMethod` | Tenant, merchant, provider customer and method references, label, brand, last four, expiry month and year, default, active. **No card number or CVV anywhere.** One active default per tenant. Nothing creates one | `PlatformTenantPaymentMethod`: same token-only fields, plus status (Active, Expired, Removed), autopay consent (who, when, text version) and removed by (D-P3) |
| `InvoiceCollectionAttempt` | Invoice, merchant, method, amount = balance, currency, origin (Automatic, Operator), status (Initiated, Pending, CapturedPendingApplication, Succeeded, Declined, Failed, Cancelled), unique idempotency key, unique provider reference, outcome text, notification sent or failed | Merged with the next row into `PlatformPaymentTransaction`: kind (charge, refund), invoice, method, amount, currency, status (Created, Pending, Requires action, Succeeded, Failed, Cancelled), idempotency key, provider ids, failure **code** (not raw text), row version |
| `PaymentTransaction`, `PaymentProviderConnection` (+ setting) | MTPPIF ledger and tenant connections; no tables, never saved; setting values would be stored as supplied | Not ported (D-P1); `PlatformPaymentTransaction` covers the ledger |
| Provider contracts | `IPlatformPaymentProvider.ExecuteAsync` (attempt, tenant, invoice, amount, currency, idempotency key, configuration reference, customer and method references) returning Pending, Captured, Declined, Failed, Cancelled; and the PIC `IPaymentProviderIntegration`. No implementation of either | One internal port with one adapter and a fake for tests; adds checkout and card-setup sessions, refund, and webhook verification; adds "requires action" (D-P1, D-P5, D-P15) |
| Webhook events | None | `PlatformPaymentProviderEvent`: provider event id (unique), type, received, processed, outcome; the source of truth (D-P5) |

V3 values fixed in code: retries on days 0, 3 and 7 after the due date; key `invoice:{id}:automatic:day-{n}`; "Payments.RegisterPayment" gates both cash and card; no test mode.

## 4. Console screens found

| V3 page | State | V4 |
|---|---|---|
| `/settings/merchant-configuration` and `/settings/platform-payment-settings` (one page, tabs Zelle and Merchant configuration) | Zelle works (revision-checked). Merchant tab saves Disabled only; Active fails with "provider not installed". Load-time permission failure is unhandled | Merchant account page (status, mode, provider, currencies, webhook health; no secret fields), step-up to change. Zelle moves to Billing settings |
| `/billing/payments` payment panel | "Charge Saved Method" shown only when a saved method and active merchant exist, so never in V3; random idempotency key per click; Pending shown as an error; no charge on part-paid invoices | Invoice page action "Charge saved card" with the balance shown, one key per invoice and request, Pending shown as processing |
| Saved cards, transactions, refunds, webhook log | None | Tenant page Billing section: saved card (brand, last four, expiry, consent), "send card setup link", remove; invoice page transactions; refund from an approved credit note |
| Public registration payment | `GET .../invoice` lists Zelle and the merchant; `POST .../pay` records whatever the browser says | Registration page opens the provider's hosted checkout; Zelle shows instructions and waits for staff |

## 5. Defects and risks seen in V3 (fixed by design in V4)

1. The public `/pay` endpoint records a Card payment for the amount, reference and date the browser sends, without asking any provider, even when no merchant is configured. It then activates the subscription and creates licences.
2. `/pay` returns the owner's account-setup link to any anonymous caller who has the registration id, and advances onboarding on a partial payment. Payment and registration commit separately, so a failure in between leaves the registration stuck.
3. No provider adapter exists, nothing can add a saved card, and the merchant account cannot be activated: V3 has never charged a card. There is no test mode, no webhook, no 3-D Secure handling and no refund.
4. Money captured but not applied: if recording the payment fails after the provider has charged the card, the attempt stays "captured pending application", staff see an error, nothing reconciles it, and it blocks every later attempt on the invoice. Pending and stuck Initiated attempts block the invoice for ever in the same way.
5. A provider timeout or exception is recorded as Failed although the charge may have gone through; the next automatic stage uses a new idempotency key, so the provider cannot prevent a double charge.
6. The console's idempotency key contains a new GUID per click; the "one open attempt per invoice" check and the insert are not atomic, so staff and automatic charges can race. A replayed request returns no payment id and a wrong balance.
7. Automatic collection charges every tenant with a default card, with no consent recorded; after a missed week it fires all three stages on three consecutive days.
8. No permission check inside the merchant and collection use cases; charging a card uses the same permission as recording cash; the refund permission is defined and unused; no step-up.
9. Raw provider exception text is stored and shown to staff; the anonymous public endpoint shows the internal merchant display name.
10. Changing the merchant's provider keeps the same account id, so saved cards from the old provider would be sent to the new one.
11. Secrets: the platform merchant stores only a reference (sound), but the paste guard knows Stripe prefixes only. The unwired MTPPIF settings would have been stored as typed, while comments promise encryption.
12. A card-paid registration invoice activates the subscription but never completes the registration (completion runs only after manual payments).
13. Schema drift: EF maps `RowVersion` and `LastConfiguredByPlatformUserId` on merchant accounts but the staging script does not create them; no script creates `PlatformPaymentSettings`.
14. Emails in English only, sent to the company email instead of a billing contact, company name falling back to "Tech Crafters"; a failed send is never retried. The onboarding receipt carries the setup link and is copied to the company mailbox.
15. No tests for card collection, idempotency or provider outcomes.

## 6. Decisions

Proposals for the owner; answers go into the Slice 5 design note.

| # | Question | Proposal |
|---|---|---|
| D-P1 | Which provider, and keep a provider abstraction? | One provider for Vixly's own merchant account (Stripe proposed: it is V3's default choice and the one its key guard knows; the owner may pick another). Keep one small internal port with one adapter and a test fake, not V3's plug-in framework. Do not port MTPPIF (tenant-owned connections) to Platform; record it as an Operations backlog item. Changing provider or account retires existing saved cards |
| D-P2 | How are cards captured? | Only through the provider's hosted checkout or hosted fields, opened from a session the API creates. Card numbers and CVV never reach Vixly servers, logs or the database, keeping Vixly in PCI SAQ A. The console never has a card-number field; staff send a "card setup link" instead |
| D-P3 | Saved cards | Store only the provider customer and payment-method ids, brand, last four, expiry and consent (who, when, which text). One default per tenant. Added by the tenant owner or billing contact through the hosted setup page (at registration or from an emailed link); removed by them or by billing staff, which also detaches it at the provider. Expired cards are not charged |
| D-P4 | Automatic collection: when, and who opts in? | One charge on the invoice's due date (with Slice 4 terms unset, that is the issue date) for tenants whose owner or billing contact ticked "charge my saved card automatically". Staff cannot opt a tenant in. Staff may also charge the saved card from the invoice page with `platform.billing.charge_cards` and step-up; amount is always the balance, never typed |
| D-P5 | Webhooks or synchronous result? | The signed provider webhook is the source of truth: the payment (method Card) is recorded and "invoice paid" raised only from a verified event, deduplicated by event id. The synchronous answer only moves the transaction to Pending or Requires action. A clock job asks the provider about transactions still open after a set time, so nothing stays stuck; an unknown outcome is never treated as Failed |
| D-P6 | Card refunds and who approves | Refund to the card, full or partial up to the amount charged, only against a credit note that the sales manager has approved (Slice 4 four eyes); the refund is sent by billing staff with step-up and recorded as a refund transaction. Refunds of hand-collected money stay as recorded by hand (Slice 4) |
| D-P7 | Registration payment | The API computes the amount from the registration invoice balance and creates the checkout with the registration access token; the browser sends no amount. The invoice is paid only after the provider confirms (D-P5); "invoice paid" then completes the registration. No setup link in any API response. Build the pending `GET /registration/invoice` here, and retire Slice 3's "record offline payment" once this works |
| D-P8 | Where are secrets kept? | Not in the database in plain text. API key and webhook signing secret come from configuration (environment variables now, Google Secret Manager later, Global Standard S-11); the merchant record holds only non-secret data (provider, mode, publishable key). If the owner wants keys entered in the console, store them encrypted with Data Protection, write-only, never shown again |
| D-P9 | Test and live mode | Mode is part of the merchant account and checked against the environment: staging accepts test keys only, production live keys only. The console shows the mode on every payment screen |
| D-P10 | Zelle | Keep as Slice 4 has it: instructions on the invoice and registration page, payment recorded by staff with a reference. Move the Zelle recipient, instructions and QR into Billing settings (per language); no separate settings entity |
| D-P11 | Currencies | Charge in the invoice currency only; the merchant account lists the currencies it accepts, and card payment is not offered for others. Amounts sent in the currency's minor units. No conversion |
| D-P12 | Permissions | `platform.billing.charge_cards` (step-up), `platform.billing.refund_cards` (step-up), `platform.payments.configure` (step-up: merchant account). Billing role gets charge; the administrator holds all; refunds need an approved credit note (D-P6) |
| D-P13 | Retries and dunning | Handed to Slice 6. Slice 5 makes one attempt and publishes "card payment failed" (invoice, reason code, retryable or not); Slice 6 owns the retry schedule (V3 used days 0, 3, 7), reminders and suspension, and retries by calling Slice 5's charge use case with its own idempotency key |
| D-P14 | Emails | Success: the Slice 4 receipt. Failure or "authentication needed": one email to the billing contact in the tenant's language with a link to the hosted page to update or confirm the card. Sent through the outbox; no provider error text in the email |
| D-P15 | 3-D Secure and disputes | When the bank asks for authentication, the transaction becomes Requires action and the tenant gets the D-P14 link; there are no silent repeats. Disputes arrive by webhook, are shown on the invoice and notify billing staff; a lost dispute reverses the payment (Slice 4 reversal) |
