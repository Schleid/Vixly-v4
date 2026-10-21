# Slice 5F: Billing, the cashier and the customer screen - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 4) |
| Date | 2026-10-20 |
| Builds on | Slice 5C (final shipments, the counter), 5D (insurance, the estimate, the page to sign), 5E (supplies) |
| Source | The owner, 2026-10-20 evening: "we keep a central cashier where payment is processed; we could also, using a tablet, have the clerk send the payment to the customer, who launches the payment… some companies may not want to hire a cashier" |
| Status | **Agreed with the owner on 2026-10-20** (payments and the invoice rules); credit accounts (F-10) and the first card provider are open. Step 1 (the invoice) is being built |

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
| P-13 | **One provider first**, then others as companies need them. Each company connects its own provider account (Vixly never holds the company's money). *Owner to choose the first provider* (question F-8). |

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

### Open with the owner

- **F-10 credit accounts:** who opens an account and sets its limit (a right, with approval?), whether a charge above the limit is refused or needs approval, the payment term (e.g. 30 days) and monthly statements, and how the customer pays the account later (the cashier, the portal). The staff customer record had its credit account section removed earlier (2026-10-20 inventory): the account would come back under its own right.
- **F-8 first provider:** the first adapter to build behind the common interface.


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

## 5. Steps

| Step | Content |
|---|---|
| 1 | The invoice from a final shipment and from a supplies order; numbers, taxes, PDF in the customer's language; corrections |
| 2 | The cashier: find, pay (cash with drawers, Zelle and other recorded methods), receipts; the payment settings; *Zelle to verify*; release on payment |
| 3 | The customer screen: pairing, the summary, methods, Zelle steps, insurance acceptance and signature |
| 4 | The first card provider (terminal and payment page) |

## 6. Not in this slice

Payments on the customer portal (later) and reports. Credit accounts: see F-10.
