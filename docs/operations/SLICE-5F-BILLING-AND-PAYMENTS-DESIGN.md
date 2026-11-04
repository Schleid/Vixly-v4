# Slice 5F: Billing, the cashier and the customer screen - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-20 |
| Builds on | Slice 5C (final shipments, the counter), 5D (insurance, the estimate, the page to sign), 5E (supplies) |
| Source | The owner, 2026-10-20 evening: "we keep a central cashier where payment is processed; we could also, using a tablet, have the clerk send the payment to the customer, who launches the payment… some companies may not want to hire a cashier" |
| Status | **Agreed with the owner on 2026-10-20** (payments and the invoice rules); credit accounts (F-10) and the first provider (Stripe) answered. Steps 1, 2, 2b, 3 and 4 built |

## 1. What the owner said

- **Two ways to take payment:** a **central cashier**, and a **customer screen** (a tablet or iPad) the clerk sends the payment to, where the customer chooses how to pay and pays. Some companies do not want to hire a cashier.
- **Each company chooses the payment methods it accepts.** Some take no cash: card or Zelle only.
- **Zelle:** the screen shows the company's Zelle details and its QR code. The shipment stays **on hold** until finance verifies the money came in, then it is released. The customer must put the **shipment number** in the Zelle memo: the screen shows a strong warning and the customer accepts the company's Zelle payment terms before seeing the Zelle details.
- Each company **uploads a picture of its Zelle QR code**; the screen also shows the **Zelle tag** and the email or phone, whichever the company has.

## 2. Payments (agreed)

### How payment is taken

| # | Decision |
|---|---|
| P-1 | **Central cashier** (`/staff/cashier`, right *take payments*): find the customer the same way as the counter (phone, email, customer code) or by shipment, order or invoice number; their unpaid invoices, each with **Pay**; the payment, the receipt. A company with no cashier gives the clerk the *take payments* right. |
| P-2 | **The customer screen:** a tablet or touch screen at the counter, signed in once as the counter's *customer screen* and paired with a clerk's desk. The clerk taps **Send to customer screen**; the screen shows, in the customer's language: **1)** the shipment's or order's financial summary (the estimate's lines, then the invoice's once issued), **2)** the payment methods the company accepts, **3)** the chosen method's steps. The clerk's screen follows each step and shows *Paid* or *Waiting for confirmation*. *Clear · next customer* resets the screen. |
| P-3 | **The same screen** is where the customer **accepts the insurance terms** and **signs** (Slice 5D), so the acceptance is the customer's own, not a tick by the clerk. The page to print stays for companies without a screen. |
| P-4 | **Card data never passes through Vixly** (it would put every company under the heaviest card-security rules, PCI DSS). Cards go straight to the payment provider: the provider's **card terminal** (the clerk's request appears on the terminal; the customer taps or inserts the card) or the provider's **payment page** (on the customer screen, or by a QR code on the customer's own phone). The provider tells Vixly when it is paid. |

### Payment methods

| # | Decision |
|---|---|
| P-5 | **Each company chooses the methods it accepts** (Settings → Payments), per company and, when it wants, per warehouse: cash, card, Zelle, and others to add (bank transfer, cheque, mobile money such as MonCash or Natcash). A method turned off never appears: with cash off there is no cash drawer. |
| P-6 | **Two kinds of method.** *Processed*: card through a provider; Vixly knows for certain when it is paid. *Recorded by staff*: cash, Zelle, cheque, bank transfer; a person records it, and for some (Zelle) another person confirms the money arrived. |
| P-7 | **Cash:** a **cash drawer** per cashier or desk, opened with a starting float and closed with a count; the difference is recorded. Change is computed on the cashier screen. Cash is never offered on the customer screen (a person takes it). |

### Zelle (agreed step by step)

| # | Decision |
|---|---|
| P-8 | **Company settings:** the Zelle **QR code** (a picture the company uploads), the **Zelle tag**, the **email or phone** enrolled with Zelle, the **name** the customer sees when paying, and the company's **Zelle payment terms** in English, French and Spanish. The screen shows only what the company filled in. |
| P-9 | **On the customer screen:** 1) the financial summary and the methods; 2) the customer picks Zelle; 3) **a strong warning and the company's Zelle terms** (the shipment number in the memo is required; the shipment is held until finance confirms the money arrived; anything the company adds); the customer must accept to go on, and the acceptance is kept (when, on which screen, the clerk present); 4) **the Zelle screen**: QR code, Zelle tag, email or phone, the exact amount and the **shipment (or order) number to write in the memo**, in large type; 5) the customer pays in their own bank app. |
| P-10 | The clerk or cashier confirms *the customer says it is sent*, with the Zelle confirmation number when the customer shows it. The shipment goes **on hold, awaiting payment confirmation**: it cannot be released, bundled or handed over. |
| P-11 | **Finance: "Zelle to verify"** (right *confirm payments*): shipment or order number, customer, amount, date, confirmation number. Someone checks the company's bank account and marks it **received** (the shipment is released) or **not received** (it stays on hold, the customer is told). Who, when and the bank reference are kept. A payment that arrives without the number is matched by amount and name by finance, who records the reference. |
| P-12 | Vixly cannot make an official Zelle QR code nor read the company's bank account: the QR code is the company's own picture, and finance's confirmation is the proof. The same flow serves any method *recorded by staff* that needs a check (bank transfer). |

### Payment providers

| # | Decision |
|---|---|
| P-13 | **One provider first**, then others as companies need them. Each company connects its own provider account (Vixly never holds the company's money). **Stripe first** (owner, 2026-10-20). |

## 3. The invoice (the owner's answers, 2026-10-20)

| # | Decision |
|---|---|
| F-1 | **One invoice per transaction.** While a shipment is processed, everything goes on the shipment's invoice: freight, the shipment's charges and extras, insurance and supplies. When no shipment is processed, the supplies ordered go on their own invoice (*Order supplies*). The shipment's invoice is made when the shipment becomes final. |
| F-2 | **Payment terms: prepaid by default**; *collect* (paid at the destination) as the company allows. **A collect shipment cannot have supplies**: supplies are paid at the counter, so the clerk orders them separately on their own invoice. |
| F-3 | **Release on payment** (industry standard and V3): an unpaid prepaid shipment is held and cannot be bundled or dispatched; collect and an account charge release it. |
| F-4 | **A shipment is paid in full** before it is released; **several payment methods may be combined** (part cash, part card). Overpayment is refused. |
| F-5 | **Taxes:** computed per line by kind (freight, insurance, supplies, extras), each with the company's rate setting; the invoice shows **one tax line per kind or a single tax line**, as the company chooses. No rate set: no tax, and the invoice says so. |
| F-6 | **Corrections:** an issued invoice is never edited. A credit note (full or partial, with a reason) or a void before any payment, requested by one person and approved by another with the right *approve credits*. Reopening a final shipment goes through a credit note. |
| F-7 | **Refunds:** card through the provider, cash from the drawer, Zelle recorded by hand; right *refund payments*, with a reason. |
| F-8 | **Payment providers: a neutral platform.** Vixly defines one way to talk to a card provider; each company connects its own provider. Providers are added one by one behind that common interface. |
| F-9 | **No freight discount at the counter.** A lower freight price is given only through a **special quote** (Slice 4). Discounts at the counter are for supplies only (Slice 5E). |
| F-10 | **Bill my account:** a customer with a credit account sees *Bill my account* as a payment method (owner, 2026-10-20). Questions below. |

### Owner's answers, 2026-10-20 (evening)

- **F-8:** the first card provider is **Stripe**.
- **F-10:** the company's **credit department** opens credit accounts and manages credit (limits, holds, follow-up): a right of its own, *manage credit accounts*, given to that department.

- **Above the limit:** a company setting: refuse *Bill my account*, or let the charge wait for the credit department's approval.
- **Terms and statements:** company settings, 30 days and a monthly statement by default; the credit department can give a customer other terms.
- **Paying the account:** at the cashier (step 2), then on the portal once Stripe comes in (step 4).

## 4. Step 1 in detail: the invoice (built from 2026-10-20)

The owner asked to start with the invoice while F-10 and the provider are open. Points marked *industry standard* follow common invoicing practice; each company sets them in its settings.

| # | Decision |
|---|---|
| I-1 | **Issued by itself**, never typed: a shipment's invoice when the shipment becomes final; an order's invoice when the clerk confirms the order. Numbered with the company's invoice prefix (INV, Settings → Numbering). One invoice per shipment and per order. |
| I-2 | **Kept as issued** (*industry standard*): the company's name and tax number, the issuing warehouse, the customer (code, name, company, phone, email), the shipment or order number, the route and service, each line, the taxes and the totals, in the **customer's language**. A later price, name or setting change never alters it. |
| I-3 | **Lines:** the shipment's freight lines as priced at final (freight per parcel, items, minimum, ground freight), its charges and extras (service fee, consolidation, extras), its insurance, then its supplies with their discount. An order has its supplies only. |
| I-4 | **Taxes (F-5):** the company sets a tax name ("Sales tax"), its tax number, and a rate for each kind: freight, insurance, supplies, charges and extras. No rate: no tax. A supply marked *not taxed* and a government fee are never taxed. The tax of each kind is worked out on that kind's total and rounded to the cent. The invoice shows one tax line per kind or a single tax line, as the company chooses (default: a single line). |
| I-5 | **Where it stands:** *to pay* (until the cashier, step 2), *partly credited*, *credited*, *voided*. What is owed = total − credits − payments. |
| I-6 | **Corrections (F-6):** an invoice is never edited. A person with the right *request credits* asks for a **credit note** (some lines, all or part of their amount, with a reason) or a **void** (only while nothing is paid). A different person with the right *approve credits* approves or refuses it. Approved, a credit note gets its number (prefix CN, Settings → Numbering) and its own PDF; the taxes come back in proportion. Credits never exceed what the invoice still holds. |
| I-7 | **A voided order** is cancelled and its stock comes back. The clerk's *Cancel* on a confirmed order now asks for the void of its invoice. A voided or credited shipment invoice leaves the shipment final; reopening a shipment comes later (F-6). |
| I-8 | **Where it is seen:** Invoices (`/staff/invoices`, right *see invoices*, per warehouse): find by number, customer or shipment, filter by state; the invoice with its lines, credit notes and PDF. The counter's shipment and order pages show the invoice once issued. **Credits to approve** lists the requests for the approvers. On the portal the customer sees their invoices and credit notes with the PDF. |
| I-9 | **Rights:** *see invoices*, *request credits*, *approve credits*, each per warehouse; Owner and Administrators hold them. A person never approves their own request. |
| I-10 | **Settings → Invoices:** the tax name, the tax number, the rates, one line or one per kind, and a note printed at the foot of every invoice in English, French and Spanish (payment terms, bank details). The currency can no longer change once an invoice exists. |
| I-11 | **Shipments made final before this step** have no invoice. |

## 5. Step 2 in detail: the cashier (from 2026-10-20)

Points marked *industry standard* follow common point-of-sale practice; each company sets them in its settings.

| # | Decision |
|---|---|
| C-1 | **Settings → Payments:** the methods the company accepts: cash, Zelle, cheque, bank transfer, and up to three others it names (MonCash, Natcash...). Each on or off. Zelle: the QR picture (PNG or JPEG), the Zelle tag, the email or phone, the name shown, the terms in EN/FR/ES. Card comes with Stripe (step 4). |
| C-2 | **The cashier** (`/staff/cashier`, new right *take payments*, per warehouse): find by phone, email, customer code, or by invoice, shipment or order number; the customer's invoices to pay at that warehouse; **Pay**. |
| C-3 | **Paid in full** (F-4): one or more tenders whose total is exactly what is owed; overpaying is refused. Cash: the amount handed over and the change, worked out on screen. Cheque and transfer: their reference. Each payment is numbered (PAY, Settings → Numbering) and prints a receipt in the customer's language. |
| C-4 | **Cash drawer** (*industry standard*): a cashier opens their drawer at a warehouse with a starting float; cash is taken only into an open drawer; closing it, the cashier counts and the difference (over or short) is recorded with a note. The drawer's report: float, cash in, change out, expected, counted, difference. |
| C-5 | **Zelle** (P-9 to P-11): the cashier records *the customer says it is sent* with the confirmation number when shown, after the customer accepted the Zelle terms (the cashier ticks that the customer accepted them; on the customer screen in step 3 the customer does). The invoice is then **awaiting confirmation**. |
| C-6 | **To verify** (`/staff/cashier/verify`, new right *confirm payments*, per warehouse): Zelle and bank transfers waiting; *received* (with the bank reference) settles them; *not received* takes the payment back, the invoice is owed again and the customer is told by email. Who and when are kept. |
| C-7 | **Where an invoice stands:** to pay, awaiting confirmation, paid. **A final shipment is released only when its invoice is paid** (F-3); the shipments desk and the counter show *unpaid*, *awaiting confirmation* or *paid*. Holding bundles and dispatch uses this when they are built. |
| C-8 | **Refunds** (F-7) come with credit notes: an approved credit note on a paid invoice is refunded in cash from a drawer, or recorded for Zelle or transfer, by a person with the right *refund payments*, with a reason. |
| C-9 | **A void of a paid invoice** is refused (F-6): a credit note and a refund instead. |

Step 2b, credit accounts (F-10), follows: the credit department's accounts (limit, terms, statements), *Bill my account*, and paying the account at the cashier.

## 5b. Step 2b in detail: credit accounts (from 2026-10-21)

The owner's answers of 2026-10-20 (F-10) with common accounts-receivable practice (*open-item* accounts: each charged invoice stays owed until paid; the statement lists them with their due dates and age). Points marked *industry standard* are Claude's reading; each is a company setting or a credit department decision.

| # | Decision |
|---|---|
| K-1 | **A credit account per customer**, opened by the **credit department** (the right *manage credit accounts*, formerly *allow customer credit*, with the second check). It has a **limit** in the company's currency, **terms** in days (empty: the company's), and a state: **active**, **on hold** or **closed**, each change with a reason, audited. Closed only once nothing is owed on it. The customer's old *credit allowed* mark follows the account (active: on). |
| K-2 | **Settings → Credit:** the company's terms (**30 days** by default) and what happens **above the limit**: *refuse* (default) or *ask the credit department*. The monthly statement is on by default. Credit itself stays switched on or off in Settings (*allow credit*). |
| K-3 | **Bill the account** at the cashier (right *take payments*): for a customer with an active account, the whole amount owed on the invoice is charged to the account, due after the terms (from the day it is charged). Within what is available, at once; above it, refused or waiting for the credit department, as the company chose. |
| K-4 | **A charged invoice releases its shipment** (F-3): it can be bundled and dispatched; it stays **owed on the account** until it is paid at the cashier like any invoice (any method, Zelle checked by finance). Its state reads *on account*; waiting for the credit department, *awaiting credit approval* (not released). |
| K-5 | **Available** = limit − owed on the account − charges awaiting approval. **Overdue**: what is past its due date. On hold or closed: no new charge; what is owed stays owed. An overdue account is not held by itself: the credit department decides (*industry standard*: shown, not automatic). |
| K-6 | **The credit department's screens:** *Credit accounts* (each account's owed, available, overdue, state; open one for a customer by code), the account (its open items with due dates and their age: current, 1-30, 31-60, 61-90, over 90 days; change its limit, terms or state; the statement), *Charges to approve* (approve or refuse with a note; a person never approves a charge they asked for). |
| K-7 | **The statement** (PDF, in the customer's language): the company, the customer, the date, every open item (invoice, reference, date, due date, amount, owed), the totals by age and the total due. **Monthly**: on the 1st, each customer with something owed gets an email saying their statement is ready, with the link to the portal. |
| K-8 | **On the portal**, *Account*: owed, available, overdue, the open items and the statement PDF. Paying the account on the portal comes with Stripe (step 4). |
| K-9 | **Corrections** work as before: a credit note lowers what is owed; a void (nothing paid) takes the invoice off the account. |

**Details settled while building** (2026-10-21): deciding a charge asks for the second check and a refusal needs a note; a charge paid at the cashier while waiting leaves the queue; an account opened during a month gets its first notice on the 1st of the next; the old *credit allowed* toggle is gone, and customers marked that way before are listed on *Credit accounts* for the credit department to open (they cannot be billed until then). Verification: [Slice 5F verification](SLICE-5F-VERIFICATION.md), section 8.

## 5c. Steps 3 and 4: the owner's answers (2026-10-21, night)

- **Next:** finish payments: the customer screen at the counter and Stripe cards, at the counter and on the portal.
- **Connecting Stripe:** each company pastes its own Stripe keys in Settings → Payments (secret key, publishable key, webhook signing secret). Vixly keeps them protected with the server's key, never shows them again (only their last characters), and never logs them.
- **A card at the counter, both ways:** Stripe's secure payment page (on the customer screen, or a QR code for the customer's own phone) **and** a Stripe Terminal card reader at the desk (tap or insert; the company registers its readers with Stripe and names them per warehouse).

Built in this order: Stripe first (the cashier, the portal and refunds all use it), then the customer screen, which shows the same payment page.

## 5d. Step 3 in detail: the customer screen (built 2026-10-22)

| # | Decision |
|---|---|
| T-1 | **Adding a screen** (`/staff/screens`, for whoever serves at the counter or takes payments at that warehouse): a name and its warehouse; Vixly shows a **code** once (10 characters, valid 15 minutes, one use). On the tablet, the clerk opens `https://<company host>/screen` and types the code; the tablet then keeps its own secret in a cookie for 400 days and needs no sign-in or licence. *New code* pairs another tablet and stops the old one at once; *Remove* stops it too. Codes and secrets are kept only as hashes; codes typed are limited per address. |
| T-2 | **Sending** (P-2): the cashier invoice has *Send to pay* and the counter shipment *Send to sign*, one button per screen of that warehouse, with what the screen shows now and *Clear · next customer*. One thing at a time: a new send replaces what was there. |
| T-3 | **On the screen, in the customer's language:** welcome; the invoice's lines and totals, what is left to pay, and the methods the customer can use there: **card** (when cards are on) and **Zelle** (when Zelle is on). Cash and the other methods a person records are not offered: *To pay in cash or another way, please tell the clerk* (P-7). |
| T-4 | **Zelle** (P-9): the strong warning (the number to write in the memo, the shipment held until the company sees the money) and the company's terms, which the customer accepts on the screen; only then the QR picture, the tag or email or phone, the name, the exact amount and the memo number in large type. The acceptance is kept (when, which screen, the clerk who sent it, the language) and **counts at the cashier** instead of the clerk's tick; the clerk's page shows it. |
| T-5 | **Card:** Stripe's secure page for what is owed, as a QR code for the customer's phone and a *Pay on this screen* link; Stripe brings the customer back to the screen. The screen says paid once Stripe confirms it (it also asks Stripe while waiting). |
| T-6 | **Signature** (P-3; owner, 2026-10-22: *the customer can also sign the insurance policy/agreement on the tablet*): the shipment's declaration (parcels, contents, values, *I declare...*) and the **insurance agreement**: the product, insured value and premium, what it covers, who covers it, the deductible, the claim deadlines and the **policy's own words** in the customer's language; or *not insured* with the company's liability text. The customer ticks that they read and accept it, then signs on the screen. The policy's words are also printed on the declaration PDF. It is the shipment's own signature (as on the clerk's pad, Slice 5D), printed on the declaration PDF, with the clerk who sent it as the one present. Only while the shipment is open. |
| T-7 | **The screen reloads itself** when the clerk sends something else, the customer pays, or it is removed; a choice made on a page that has changed meanwhile is refused (*The screen changed meanwhile*). |

## 5e. Step 4 in detail: Stripe cards (built 2026-10-22)

Points marked *industry standard* follow Stripe's own guidance for platforms whose customers bring their own account.

| # | Decision |
|---|---|
| S-1 | **Settings → Payments → Cards:** the company pastes its Stripe **secret key** (or a restricted key), **publishable key** and **webhook signing secret**; Vixly checks the key with Stripe and shows the account's name. Keys are kept protected with the server's key and never shown again (only the last characters). The page gives the webhook address to add in Stripe (`https://<company host>/hooks/stripe`, the events `checkout.session.completed`, `checkout.session.async_payment_succeeded`, `checkout.session.expired`, `payment_intent.succeeded` and `payment_intent.canceled`). *Accept cards* turns cards on or off; *Disconnect* removes the keys. |
| S-2 | **Card data never passes through Vixly** (P-4): the customer pays on **Stripe's secure page** (Checkout) or on a **Stripe Terminal reader**; Vixly only learns *paid* and Stripe's payment reference. |
| S-3 | **At the cashier:** *Card: payment page (QR code)* shows a QR code the customer scans with their own phone; *Card on <reader>* sends the amount to that reader (readers are named per warehouse in the settings with their `tmr_` id). The page refreshes itself and says *Paid* when Stripe says so. The whole amount owed is charged (F-4); one charge at a time per invoice; starting another cancels the earlier one at Stripe first. |
| S-4 | **On the portal:** *Pay by card* on an invoice owed opens Stripe's page; back from Stripe, the portal says paid once Stripe confirms it. A customer only ever pays their own invoices. |
| S-5 | **Paid only on Stripe's word:** the signed webhook (checked with the company's signing secret, refused when stale) or Vixly asking Stripe directly. Recorded once, as a card tender with Stripe's reference; test-mode payments never settle a live account and the reverse. A payment for an invoice already settled another way is kept as *paid, not applied* for finance to refund in Stripe. |
| S-6 | **Cancel** expires the page or cancels the payment at Stripe first; if Stripe already took the money, the payment is recorded instead. Disconnecting or changing the account first closes what is still open on it, or refuses. |
| S-7 | **Refunds by card** (F-7) go back through Stripe: kept first as *pending* (the amount is reserved), then Stripe is asked once (the refund is its own key). Stripe refusing removes it; Stripe not answering leaves it pending with *Finish the refund* on the invoice, which asks Stripe again with the same key (Stripe never refunds twice). |
| S-8 | **Partner agents** never see or start card payments at a partner warehouse that does not handle money (Slice 7 step 4). |

## 6. Steps

| Step | Content |
|---|---|
| 1 | The invoice from a final shipment and from a supplies order; numbers, taxes, PDF in the customer's language; corrections |
| 2 | The cashier: find, pay (cash with drawers, Zelle and other recorded methods), receipts; the payment settings; *Zelle to verify*; release on payment |
| 2b | Credit accounts: the credit department's accounts, limits and terms, *Bill my account*, statements, paying the account |
| 3 | The customer screen: pairing, the summary, methods, Zelle steps, insurance acceptance and signature |
| 4 | The first card provider (terminal and payment page) |

## 7. Not in this slice

Reports. Paying a whole credit account at once on the portal (each invoice can be paid by card). Credit accounts: see F-10.
