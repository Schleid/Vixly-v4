# Control backlog of 2026-10-22: maintenance, white label, theme marketplace

The owner's items for Control (Platform), recorded 2026-10-22, to be scheduled. The Operations menu changes of the same day (four chapters, *Customer drop-off*, *Warehouse reception*) shipped separately.

| # | Item | Status |
|---|---|---|
| 1 | Maintenance windows | **Built 2026-10-23** in the applications ([Control: job records and maintenance](CONTROL-JOBS-AND-MAINTENANCE-2026-10-23.md)); the Cloudflare Worker page for deploys is still to design |
| 2 | White label | Important: needed to offer Operations under white label. Model and responsibilities decided (section 2.3); order open |
| 3 | Theme marketplace | Idea to explore; not urgent |

## 1. Maintenance windows

Owner's words: "Site maintenance management where IT can publish maintenance date that will go on all websites. When on maintenance traffic will be redirected to under maintenance site with duration info." The owner confirmed the outline below on 2026-10-22: "You get it right."

- **Who:** Vixly's IT team publishes a window in Control: a start, an end, a short message, and which sites it affects. The team decides the sites for each window: the Public site, Control, company websites, customer portals, staff workspaces.
- **Before the window:** the affected sites show an advance notice banner with the date and the expected duration.
- **During the window:** every visitor to an affected site sees a branded "Under maintenance" page with the expected end.
  - **Served from Cloudflare:** the page comes from the Cloudflare Worker planned for the side-by-side rollout (project document *V4 side-by-side rollout on vixly.ltd*), not from the applications. An application being deployed is off and cannot answer for itself.
  - **Test access:** Vixly IT can still get through.
- **Brand:** company websites and portals show the company's brand on the page and never "Vixly" (ADR-0012 W-1).
- **To design:**
  - how Control hands the window to the Worker (Workers KV, written by Control);
  - how IT gets through during the window;
  - the history of windows;
  - who may publish (a new `platform.maintenance.*` right, with step-up).

## 2. White label

Owner's words: "This one is important as we need offer Vixly operations under white label we have to figure out how we are going to do so we get operations ready."

### 2.1 What is already decided (ADR-0012, 2026-10-13, amended 2026-10-14)

- **What is white-labelled:** Operations and the surfaces it serves (staff workspace, company website, customer portal, emails, documents). Control is never white-labelled; the Public site is always Vixly's.
- **The brand:**
  - It is Operations data, edited by the company's own administrators: name, logos, favicon, app icon, theme preset, allowlisted colours, email sender name and reply-to, support contacts, legal footer.
  - Draft and publish; no raw CSS.
- **The White Label bundle** is a plan feature, sold, invoiced and collected by Control. It covers:
  - no "Powered by Vixly";
  - a custom domain;
  - a custom email sending domain;
  - the app title and icons.

  When a company loses the bundle, its data is kept and the surfaces fall back.
- **Custom domains:** verified before use, TLS at Cloudflare. A passkey is bound to its host, so set the domain before the company's users enrol.
- **Emails:** they always carry the company's sender name. They go from a neutral sending domain, never a Vixly one, until the company's own domain is verified (SPF, DKIM).

### 2.2 Built and missing (code review, 2026-10-22)

| Part | State |
|---|---|
| No "Vixly" in tenant-facing text (W-1) | Built and checked by the build |
| Company brand: name, logo, favicon, colours; website editor with draft and publish | Built (`OperationsTenantBrand`) |
| Plan features `CustomDomain` and `WhiteLabelPortal` | Exist in Platform; **nothing in Operations reads them yet** |
| Custom domain per company (W-5) | **Not built.** Operations finds the company only from `{slug}.{base domain}` |
| Custom email sending domain (W-7) | **Not built.** Operations sends from one configured address |
| Email sender name and reply-to per company | To check against W-2 (the sender name is the company's name today) |
| App title and icons, "Powered by" removal | To check: no "Powered by" text found in Operations or Customer resources |
| Neutral base domain (W-8) | **Owner decision open** |
| Control's part (sell the bundle, see who has it, follow domain verification) | Sale and billing built (plan features); no Control view of brands or domains (by design, Amendment 1) |

### 2.4 Moving a company to its own domain (owner, 2026-10-22, 13:29)

Owner's words: "moving from galeo.vixly.ltd to galeologistics.net would need a mechanism to automatically redirect users with a message telling, galeo logistics has moved from galeo.vixly.ltd/customers to galeologistics.net/customer. Public portal is available at galeologistics.net." It is the same process for every company.

After the switch-over, the old address `{slug}.vixly.ltd` keeps answering. It never shows the old pages again; instead:

- **A "has moved" page** carries the company's brand (never "Vixly") and is in the visitor's language. It names the new addresses:
  - the customer portal (`{domain}/customer`);
  - the company website (`{domain}`);
  - the staff workspace (`{domain}/staff`).
- **The redirect:** after a few seconds the visitor goes on to the same page at the new address, with a button to go at once. A deep link keeps its place: `{slug}.vixly.ltd/customer/invoices` leads to `{domain}/customer/invoices`.
- **Search engines and programs** get a permanent redirect (308) instead of the page. Webhook and API callers are told the new address.
- **Duration:** the notice shows for a transition period, then the old address redirects silently. The period is a **per-company setting** (owner, 13:31). Staff and customers are also told by email at the switch-over.
- The same mechanism serves a company that later leaves white label and returns to `{slug}.vixly.ltd`.

### 2.3 Owner decisions (2026-10-22, 12:50 to 13:25)

**Correction (2026-10-22):** company staff sign in with an authenticator app, not a passkey. The authenticator keeps working on a new address. What does move with the address:
- the sign-in and licensed-device cookies, which are bound to their host;
- so each computer registers again on the company's own domain, and everyone signs in once more.

The move to a company's own domain therefore needs a planned switch-over. It is not a reason to do it before the first sign-in.


1. **No neutral address.** A company without a white label contract keeps `{slug}.vixly.ltd`. A company under white label works on its own domain. This settles ADR-0012 W-8.
2. **Model (a) only.** Each company presents Operations under its own brand, as ADR-0012 describes. There is no reseller or partner level.
3. **Control's part:** "Everything that falls under their responsibility to get the company under white label to work". For example:
   - **the request first** (owner, 12:56): a company asks for white label and Vixly analyses the submission, asks for the proper documents and verifies them before anything is switched on. Examples: the company's registration, its right to the brand and name it will show, proof that it owns the domain. The request is approved or refused with a reason, and every step is kept;
   - the contract (the bundle on the plan), once the request is approved;
   - onboarding the company's domain and email domain: the DNS records to give the company, verification, certificate status;
   - a status board per company;
   - support actions: re-check, suspend the custom domain, fall back to `{slug}.vixly.ltd`;
   - the history of all of it.
4. **Order** (owner, 13:25): white label is not for day one. "We are getting Vixly ready and we will wait until Galeo's admin decides. We are building Vixly for that and preparing the requirement for any company requesting white label to follow." The pilot company starts on `{slug}.vixly.ltd`. White label is built as a capability with published requirements for any company; nothing waits on one company's date.
5. **Email** (owner, 13:12): "If the company goes white label, I think it will be using its own email service not the one Vixly works with." Direction under discussion: the company connects its own email service, as it already connects its own Twilio for texts. Until it is connected, or when it fails, emails go out with the company's name from the shared address (W-7).
6. **The pilot company's facts, for when it asks** (owner, 13:20 and 13:25):
   - domain `galeologistics.net`;
   - the **full domain** is required (owner rule for every company, not a subdomain);
   - hosting and email at SmarterASP.net, email `@galeologistics.net` from that hosting;
   - registrant as shown in WHOIS.

## 3. Theme marketplace (idea)

Owner's words: "it's not just staff, we can have any dev to create theme and submit. Vixly admin price it and make it available for company to purchase for their landing page." Themes are for both the company website and the customer portal (owner, 2026-10-22).

The shape as discussed, to explore later:

- **Developers:** any developer (an outside designer, not only Vixly staff) submits a theme. A theme is a package of allowlisted design tokens, layouts and images; it never contains raw CSS, HTML or script, so ADR-0012 W-3 holds.
- **Vixly admins in Control:** they review a submission (accessibility, safety, quality), set its price, publish or retire it, and see its sales.
- **Companies in Operations:** they browse the gallery, preview a theme on their own content, buy it (billed by Control like a plan charge) and apply it to the website and the portal. This needs the `PortalThemeCustomization` plan feature.
- **Open questions:**
  - developer accounts and agreements;
  - revenue share and payouts;
  - refunds;
  - theme updates after purchase;
  - versions;
  - whether free themes exist.
- **Where it sits:** ADR-0017 placed the theme marketplace in Platform as Slice 10, after the migration.
