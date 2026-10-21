# Slice 2: Customers and mailboxes - verification record

| | |
|---|---|
| Plane | Operations and Customer (Stage 3) |
| Date | 2026-10-18 |
| Built on | Slice 1E, staging and W-4, the demo staff and the shipping services (delivered the same day); not committed here: the dev team owns git |
| Design | [Slice 2 design note](SLICE-2-CUSTOMERS-DESIGN.md), decisions D-C1 to D-C5; ADR-0019 |
| Status | **Built and verified by automated tests; waiting for owner sign-off** |

## 1. What was built

### Customer portal (`{slug}.<base>/customer`, Customer.Web)

| Page | What it does |
|---|---|
| `/customer` | Signed in: "Hello Ana", the customer code and every mailbox address. Signed out: the sign-in page |
| `/customer/sign-up`, `/sign-up/sent` | Name, company (optional), email, phone with the country code, language and password. The bot check (Cloudflare Turnstile) runs before anything is sent |
| `/customer/verify?token=` | Shows a "Confirm my email" button; the click confirms, gives the code and signs in. Link scanners that only open the link use nothing |
| `/customer/sign-in`, sign-out | Email and password. An unconfirmed customer with the right password is told to confirm and can ask for a new link |
| `/customer/forgot-password`, `/reset-password?token=` | Reset by email |
| `/customer/profile` | Name, company, phone, language (the pages switch at once); change the password |
| `/customer/culture/{en,fr,es}` | Language switch; returns only to a `/customer/` address |

- **Brand:** every page shows the tenant's published name, logo and colours (with the plan fallbacks of W-4). No page and no email says "Vixly".
- **Forms:** static forms with antiforgery. Every POST checks the token, also when the form has no other field. Pages are never cached.
- **Errors:** Operations' error codes become the customer's own words, with their values ("Use at least 12 characters."). An outage says "The service is not available right now".
- **Security headers:** the portal's content security policy allows only the Turnstile script and frame from outside the site.
- **Look:** a placeholder stylesheet (`customer-portal.css`). Frontend/UI designs the final look.

### Accounts (Operations)

- **Codes (D-C1):** the tenant's customer prefix (new setting, default `C`, on the settings page) and at least five digits: `C00042`.
  - A customer gets the code when the email is confirmed.
  - A refused form never uses a number.
- **Mailbox addresses (D-C2):** computed for every active receiving warehouse that accepts deliveries. The first line is the customer's name and code.
- **Sessions (D-C3, D-C4):**
  - server records, 60 idle minutes, 12 hours at most;
  - checked on every call, so a suspension, closure or password reset applies at the next page;
  - the cookie `__Secure-vx4-customer` holds only the token (HttpOnly, Secure, SameSite=Lax, path `/customer`).
- **No enumeration:**
  - Sign-up and forgot password give the same answer for known and unknown emails. A known customer receives "you already have an account".
  - Unknown, suspended and closed customers get the same sign-in message as a wrong password.
  - Every path checks a password hash, so they take the same time.
- **Lockout:** 5 failures lock the account for 15 minutes.
- **Links:** single use, stored as hashes. They last 24 hours (confirmation) or 60 minutes (reset). A new link of the same kind cancels the older ones.
- **No second factor for customers (D-C5).**

### Staff side (`/staff/customers`, Operations.Web)

- **Permissions:** `ops.customers.view`, `.manage` and `.credit`.
  - The Owner and Administrators hold them.
  - Tenant roles can include them (new "Customers" area on the role form).
  - Only a grant for the whole company counts: branch and warehouse grants give none of them.
- **List:** search by name, email, phone or code; 25 per page.
- **No creating or editing (owner, 2026-10-18, D-C6):** customers register themselves; their details are their own. The staff pages show them and offer no form for them.
- **Customer page:**
  - details and mailbox addresses (read only);
  - suspend, reactivate or close, with a reason. This ends the customer's sessions;
  - allow or remove credit, with a reason (`ops.customers.credit`, step-up).
- **History:** new areas "Customers" and "Shipping services" in `/staff/audit`. Every change writes `ops.customers.*`.
- **Menu (owner, 2026-10-18):** Workspace (Home, Customers), Business setup (Locations, Shipping services, Website, Settings), Team and access (Staff, Roles, Devices), Company (Company, History). **Billing** (the company's own subscription bill from the platform) and **Terms** are tabs of the Company page, beside its profile. The `/staff` home lists the same links in the same order.

### Operations.Api (internal, caller `customer`)

- `GET /internal/v1/customers/brand`, `POST sign-up`, `verify`, `verification`, `sessions`, `password-reset`, `password-reset/complete`, `DELETE sessions/current`, `GET` and `PUT me`, `POST me/password`.
- Each needs a tenant in the caller token. The session travels in `X-Customer-Session`; the customer's IP address and browser in `X-Customer-Ip` and `X-Customer-Agent` (kept on the session).

### Development routing (owner, 2026-10-18)

- In Development only, Operations.Web passes `/customer/**` to Customer.Web (`OperationsDevelopment:CustomerPortalUrl`, `https://localhost:7406/`), with the original host name, as Edge does in production.
- `https://andes-courier.localhost:7404/customer/sign-up` therefore works when Customer.Web runs too.

### Data and texts

- Migration `Customers`:
  - tables `Customers` (unique email and unique code per tenant), `CustomerSessions` and `CustomerTokens`;
  - the column `TenantSettings.CustomerPrefix`.
- `OperationsText`: 938 to 1,019 keys in en, fr and es (staff pages, permission names, errors, the four customer emails).
- `CustomerText`: 9 to 79 keys.
- No "Vixly" in any of them.

## 2. Automated checks

Release builds of the three solutions: no errors, no warnings.

| Test project | Slice 1E | Slice 2 (with W-4, demo staff and shipping services) |
|---|---|---|
| Operations.Domain.Tests | 74 | 90 |
| Operations.Application.Tests | 63 | 65 |
| Operations.Infrastructure.Tests (SQL Server) | 41 | 51 |
| Operations.Api.Tests (now with SQL Server) | 14 | 19 |
| Operations.Web.Tests (SQL Server) | 40 | 44 |
| Customer tests | 20 | 40 |
| Platform tests | 665 | 667 |
| Public.Web, Shared.Kernel, Shared.UI | 113 | 113 |
| Architecture.Tests | 8,444 | 8,862 |

All pass. The global SQL script (28 migrations) runs twice on an empty server, and twice over a database built with the script of the demo-staff delivery, without errors.

What the new tests cover:

- **End to end: the real portal, the real Operations.Api and a real database:**
  - Ana signs up; before confirming, the right password says "confirm first";
  - she confirms from the emailed link, sees her code, the North warehouse address with "Ana Pierre C00001", and nothing of the South tenant;
  - the same link a second time is refused;
  - she signs out and in again.
- **Same answers:** a known and a new email get the same sign-up answer; a known and an unknown email the same forgot-password answer. The known customer receives "you already have an account"; nobody is emailed for the unknown address. The reset ends the old session.
- **Suspension:** a suspended customer is signed out at the next page and cannot sign in.
- **Two tenants:**
  - the same email exists in both;
  - North's session cookie on South's host is not a session;
  - a customer of North only cannot sign in to South.
- **Staff pages over HTTP:**
  - the Owner finds a self-registered customer by code and sees the details read only; no route accepts customer details from staff (create and edit answer 404 or 405);
  - suspending (a reason is required) and credit work and appear in the history;
  - the menu shows "Team and access", and Billing and Terms only as tabs of Company;
  - a website editor sees "Your role does not allow this." and no customer.
- **Portal over HTTP (fake Operations):**
  - brand and no "Vixly";
  - a POST without the token answers 400;
  - the bot check runs before anything is sent;
  - the code and mailbox appear after sign-in;
  - error values are filled in;
  - the confirmation link only shows a button;
  - a refused session clears the cookie;
  - the language switch never leaves `/customer/`;
  - an outage is explained.
- **Bot check:** no token or no secret fails without asking Cloudflare; an outage or `success: false` fails; turned off, it passes.
- **Domain:**
  - confirmation gives the code once;
  - details are checked before a code is taken;
  - lockout for 15 minutes after 5 failures;
  - status moves and reasons;
  - credit reason;
  - idle and absolute session ends;
  - links once, per purpose, until they expire.
- **Database:**
  - round trip within the tenant;
  - one email and one code per tenant, the same email in two tenants;
  - search by name, email, phone or code with paging.

## 3. Independent security review (2026-10-18)

A separate reviewer read the Slice 2 and shipping-services code without having written it. It found no high-risk problem. Its findings were fixed before delivery, each with a test:

- **Sign-up told known emails apart** when another field was wrong (a known email got "check your email", a new one "phone invalid"). Every field is now checked before the email is looked up.
- **Failed sign-ins sent at the same time** were mostly not counted, so the lockout allowed more than 5 guesses. Each failure is now counted by one database statement. Test: 5 failures at once lock the account.
- **Older links survived a new password:** an invitation or reset link asked for before a reset or change could set the password again. A new password now cancels every open reset link.
- **Wrong current passwords on the profile page** never locked. They now count like sign-in failures; at the lockout every session ends.
- **Emailed links** used the portal name sent by the caller without checking it. Only a host label is accepted now, so a link never leaves the tenant's host.
- **Names in emails:** names and company names may not contain `<`, `>`, `@` or a web address (`ops.customers.name_invalid`), so a sign-up cannot put a link into an email.
- **A new sign-in** now ends the session already in the cookie.
- **A malformed session cookie** caused an error page; it is now ignored.
- **A huge page number** on the staff list caused an error; pages and search text are capped.
- **Two prefixes could not swap** in one save (for example shipments `C`, customers `CU`). All five are now checked together.
- **An empty or over-long password** at sign-in is refused before the look-up, the same way for every email.

**Not changed** (low risk, noted):

- A failed save after a customer code was taken leaves a gap in the numbering. Codes stay unique; they are not guaranteed to be consecutive.
- A wrong password for a real account writes a history entry; an unknown email does not. The extra database write is a small timing difference, behind the bot check, the rate limits and the lockout.

## 4. Where the build differs from the design note

- **The tenant site test** (`TwoTenants_ShowTwoSites_AndNeverVixly`) now ignores the shared UI library's asset addresses (`_content/Vixly.Shared.UI/vixly-theme.css`). They are file addresses, not text a visitor sees (owner, 2026-10-18: "the best and most efficient way"). Every visible text is still checked.
- **Account fields** live on the `Customers` row, not in a separate owned record.
- **The customer's "copy" button** for the mailbox address is a Frontend/UI addition; the address is plain text that can be selected.
- **Staff create and edit** were in the design note and were removed after the owner's review (D-C6). Migration `CustomersSelfService` drops the unused `CreatedByUserId` column.

## 5. Deployment notes

- **Database:** run the newest global SQL script (migrations `ShippingServices`, `Customers` and `CustomersSelfService`).
- **Customer.Web:**
  - `CustomerOperations:CallerKeyId` and `CallerPrivateKey` (secret);
  - `CustomerBotCheck:SiteKey` and `SecretKey` (secret), the Turnstile keys of the tenant wildcard host.
- **Operations.Api:**
  - `OperationsInternal:Callers:customer` with scope `customer` and the matching public key;
  - `OperationsLinks:PortalUrlTemplate` (already set);
  - `OperationsMail` (already set).
- **Development:**
  - Operations.Api trusts the development key `dev-customer` (public key in `appsettings.Development.json`);
  - the private half goes into Customer.Web's user secrets (`CustomerOperations:CallerPrivateKey`);
  - Turnstile uses Cloudflare's published test keys, which always pass;
  - **Operations.Api needs the Platform caller key too** (`OperationsPlatform:CallerKeyId` and `CallerPrivateKey` in its own user secrets, the same values as Operations.Web's). Without them every `/customer` page says "Please try again" (Operations.Api answers 503 to the tenant lookup);
  - start Platform.Api (Platform solution), and Operations.Api, Operations.Web and Customer.Web (Operations solution). Customer.Web opens no browser tab; open the portal through a company host, for example `https://andes-courier.localhost:7404/customer/sign-in`.
