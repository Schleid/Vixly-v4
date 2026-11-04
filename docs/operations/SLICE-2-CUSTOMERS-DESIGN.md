# Slice 2: Customers and mailboxes - design note

| | |
|---|---|
| Plane | Operations and Customer (Stage 3, ADR-0017) |
| Date | 2026-10-18 |
| Builds on | Slices 0 and 1 (staff, settings, branches and warehouses, brand), [Operations plane](OPERATIONS-PLANE.md) section 3 |
| Status | **Built with the owner's go-ahead to continue (2026-10-18).** Proposed decisions are marked D-C; the owner may change them at sign-off |

## 1. Outcome

- A tenant's customers sign up themselves at `{slug}.<base>/customer`, confirm their email, sign in, and see their **mailbox address**: the tenant's receiving warehouse address with their personal code.
- Staff see, create and look after customers in Operations: `/staff/customers`.
- Nothing about shipments yet (Slice 3 onwards).

## 2. Data (schema `ops`; every table carries `TenantId`)

| Class | Table | Notes |
|---|---|---|
| `OperationsCustomer` | `Customers` | Code (the casillero code), first and last name, company name (optional), email (unique per tenant), phone (international form), preferred language, status (Pending verification, Active, Suspended, Closed) with reason and time, **credit allowed** flag with reason, created by (self or staff) |
| (account fields) | columns of `Customers` | Password hash, failed attempts and lockout end, email verified time, last sign-in |
| `OperationsCustomerSession` | `CustomerSessions` | Token hash, created, last seen, idle deadline, absolute expiry, IP, user agent, ended time and reason (D-C3) |
| `OperationsCustomerToken` | `CustomerTokens` | Email verification (24 hours) and password reset (60 minutes); hash only, single use |

- **Customer code** (D-C1): the tenant's customer prefix (new setting, default `C`) followed by a number from the tenant's sequence (kind `Customer`, one sequence for all years), at least five digits: `C00042`. The code is given when the email is confirmed, so abandoned sign-ups never use codes.
- **Mailbox address** (D-C2): computed, not stored. For every active receiving warehouse that accepts deliveries:
  - first line: the customer's name and code, for example `Ana Pierre C00042`;
  - then the warehouse's own address lines, city, region, postal code and country.

  A new receiving warehouse therefore appears for every customer at once.

## 3. Rules

- **Sign-up:**
  - Fields: name, email, phone, password (the staff policy: 12 characters or more) and language.
  - A bot check (Cloudflare Turnstile, ADR-0019) runs before anything is sent.
  - The same answer is given whether or not the email already exists. An existing customer receives an "you already have an account" email instead (no enumeration).
- **Email confirmation:** a single-use link to `/customer/verify?token=…`. Confirming activates the account, gives the code and signs the customer in.
- **Sign-in:**
  - Email and password. Five failures lock the account 15 minutes.
  - A customer who has not confirmed is told to confirm and can ask for a new link.
  - Suspended and closed customers cannot sign in, and get the same message as a wrong password.
- **Sessions** (D-C3): server records in Operations. A session ends after 60 idle minutes and after 12 hours, at sign-out, on a password change or reset, and when staff suspend or close the customer. The Customer.Web cookie holds only the session token.
- **Forgot password:** the same answer for unknown emails; a single-use link for 60 minutes; the new password ends every session.
- **Staff side** (new permissions `ops.customers.view`, `.manage`, `.credit`; the Administrator holds them; branch and warehouse grants do not give them):
  - list (search by name, email, phone or code; paged);
  - customer page with the mailbox addresses;
  - **no creating or editing customers** (owner, 2026-10-18, D-C6): a customer's details come only from their own registration and profile;
  - suspend, reactivate or close, with a reason; this ends the customer's sessions;
  - allow or remove credit, with a reason (`ops.customers.credit`). Used by the payment gate from Slice 9.
- Every change writes an audit entry (`ops.customers.*`).

## 4. Customer side (Customer.Web)

| Page | What it does |
|---|---|
| `/customer` | Signed in: the mailbox addresses (copy button), the code, links to profile and sign-out. Signed out: goes to sign-in |
| `/customer/sign-up`, `/customer/sign-up/sent` | Self sign-up with the bot check; "check your email" |
| `/customer/verify?token=` | Confirms the email and signs in |
| `/customer/sign-in`, `/customer/sign-out` | Email and password; sign-out ends the session |
| `/customer/forgot-password`, `/customer/reset-password?token=` | Reset by email |
| `/customer/profile` | Name, company, phone, language; change password |

- Every page shows the tenant's **published brand**: name, logo and colours, falling back as the plan says (W-4). It never shows "Vixly".
- Static forms with antiforgery and the form guard (ADR-0019), in en, fr and es.
- **How Customer.Web reaches Operations:** Customer.Web calls Operations.Api with its signed caller token (scope `customer`, tenant id).
  - Signed-in calls also carry the session token. Operations checks the session on every call, so a suspension or a reset takes effect at once (D-C4).
- **Development routing** (owner, 2026-10-18): without Edge, `{slug}.localhost:7404/customer/…` reaches Operations.Web. In Development only, Operations.Web passes `/customer/**` to Customer.Web (`https://localhost:7406`), as Edge will in production.

## 5. Operations.Api (internal, caller `customer`)

| Endpoint | Purpose |
|---|---|
| `GET /internal/v1/customers/brand` | Published brand for the portal pages |
| `POST /internal/v1/customers/sign-up` | Self sign-up (Customer.Web has run the bot check) |
| `POST /internal/v1/customers/verify` | Confirm the email; returns a session |
| `POST /internal/v1/customers/verification` | Send a new confirmation link |
| `POST /internal/v1/customers/sessions` | Sign in; returns a session |
| `DELETE /internal/v1/customers/sessions/current` | Sign out |
| `POST /internal/v1/customers/password-reset`, `/password-reset/complete` | Forgot and reset password |
| `GET /internal/v1/customers/me`, `PUT /internal/v1/customers/me`, `POST /internal/v1/customers/me/password` | Profile, mailbox addresses, password change |

Each endpoint needs a tenant in the caller token. The session token travels in the `X-Customer-Session` header.

## 6. Anonymous endpoints and rate limits (S-6)

| Endpoint | Limit |
|---|---|
| `POST /customer/sign-up` | 5 per hour per IP, bot check |
| `POST /customer/sign-in` | 10 per minute per IP; lockout after 5 failures |
| `POST /customer/forgot-password`, `/customer/verify/resend` | 5 per hour per IP; the same answer for unknown emails |
| `POST /customer/verify`, `/customer/reset-password` | 10 per hour per IP; single-use links |

## 7. Proposed decisions

- **D-C1** Customer codes: the prefix setting and a five-digit minimum, given at confirmation.
- **D-C2** Mailbox address computed from the receiving warehouses, the name line carrying the code.
- **D-C3** Customer sessions: 60 idle minutes, 12 hours at most.
- **D-C4** Operations checks the customer session on every call.
- **D-C5** No second factor for customers in this slice.
- **D-C6** (owner, 2026-10-18) Staff never enter or change customer details. Customers register themselves; staff see them, suspend, reactivate or close them, and allow credit.

## 8. Done when

- A customer signs up, confirms, signs in and sees the mailbox address with the code. HTTP tests run through Customer.Web and Operations.Api with a real database.
- Unknown and existing emails get the same answer at sign-up and at forgot password.
- A suspended customer's session stops working at the next call.
- Tenant A's customer cannot sign in to, or see anything of, tenant B. The same email may exist in both tenants.
- Staff need `ops.customers.view` and `.manage`; credit needs `.credit`; branch and warehouse grants give none of them.
- en, fr and es complete; no "Vixly" on any customer page or email.
