# Review items of 2026-10-21: design and verification

The owner's review list of 2026-10-21, as decided with the owner and as built. Packages 89 (items 1 to 3) and 90 (items 4 and 5).

## Owner's decisions

| # | Item | Decision (owner's words in quotes) |
|---|---|---|
| R-1 | Bundles | Staff may build a bundle's list from the computer; warehouse staff scan each package to confirm. Show Entered, Scanned, Missing. "Both options possible": scan-only stays. A scanned parcel not on the list is refused with a clear message. A bundle cannot close with missing parcels. |
| R-2 | Pricing | A *Pricing* menu on the customer portal is mandatory: tiers or bands, price per kg or lb. "Do not change the pricing engine, on customer portal we show both": the regular prices and the customer's own. Public site: a company setting, and "when creating a price company can click publish to public". |
| R-3 | Staff email | "Refused at every company": a staff member's work email is never a customer email, anywhere. A different address is fine. |
| R-4 | Devices | Operations: device name, licence, browser, date and time, authorised by. Control: device name, licence, operating system, browser, IP address, date and time. At registration, the type: desktop, laptop or mobile. |
| R-5 | Member activity | Operations and Control: sign-ins, sign-outs, locations. Location: "Cloudflare country". A device signing in from another country than the one it was registered in: "Flag and ask a second factor". "We keep it simple for now." |

## R-1 Bundle lists

- A bundle may carry a list (`ops.BundleItems`): parcels and closed repack boxes, added at the computer (`ops.bundles.manage`) from what the bundle could take now, the same rules as a scan.
- A parcel or box is on one list at most (unique filtered indexes). A list holds at most as many lines as a bundle holds parcels (2,000).
- With a list, a scan of anything not on it is refused (`ops.bundles.not_on_list`); a scan of a listed line marks it scanned; taking it out marks it unscanned again.
- Close is refused while a line is missing (`ops.bundles.items_missing`). Void removes the list.
- A line that can no longer be scanned (its shipment changed, unpaid) is shown as blocked.
- A parcel on a list is not offered elsewhere and cannot go into a repack box.

## R-2 Pricing

- The pricing engine is unchanged; the pages show the same lists it rates with.
- Customer portal, `/customer/pricing`: the regular lists of the services the company offers, with a version in force, and the customer's own lists first. Never another customer's.
- Public site, `/{language}/pricing`: only when the company turns on *Show prices on the public website* (Settings) **and** a regular list is marked *Published*. A customer's own list is never published. The menu asks only whether something is published; only the page reads the lists. Shown in the sitemap.
- Dates are in the company's time zone. Publishing is recorded in the audit.

## R-3 Staff and customer emails

- A work email (a staff member who accepted the invitation and is not deactivated, at any company) never opens a customer account; a confirmed customer email (not closed, at any company) never becomes a work email.
- The checks answer only yes or no, across companies (`IsStaffEmailAnywhereAsync`, `IsCustomerEmailAnywhereAsync`; the only reviewed `IgnoreQueryFilters` reads, kept so by an architecture test), with an email index each.
- Where the answer is given, only the mailbox's owner sees it, so no company learns who is a customer elsewhere:
  - **Sign-up:** the same screen as always; the email explains (`email_not_allowed`).
  - **Confirming a customer email:** refused (`ops.customers.email_unavailable`).
  - **Accepting a staff invitation:** refused (`ops.staff.email_unavailable`). The invitation itself is never refused.
  - **Making a former member active again:** refused.
- Not checked: the Owner, named by the platform when the company is opened.
- Known limits: an alias (`name+x@`) is another address; closed companies' accounts still count.

## R-4 Devices

- At registration the type is Desktop, Laptop or Mobile (`desktop`, `laptop`, `phone`); earlier types stay valid.
- Platform keeps where a device was registered from (address, browser's user agent, Cloudflare country) and who approved it (none: the Owner's email link). Approvers of earlier devices are filled from their history.
- **Operations** (`/staff/devices`) shows name, licence, type, browser, registered date and time, authorised by. Never the address or the system.
- **Control** (`/devices`, `/devices/{id}`) also shows the operating system, the address and the country.

## R-5 Member activity and the country check

- **Country:** `CF-IPCountry`, read only from Cloudflare's addresses (`Hosting:TrustedProxyNetworks`), before the forwarded headers.
- **Activity:** every sign-in keeps its country and, in Operations, the licensed device it came from.
  - Operations `/staff/activity` (`ops.audit.view`) lists sign-ins with how and when they ended (signed out, ended by someone, idle, 12 hours, country changed), device, browser, country. Refused sign-ins and wrong passwords are in the history (link on the page).
  - Control `/activity` (`platform.audit.view`) lists sign-ins with address, country, browser, system.
- **The country check (Operations):**
  - A device with a country of its own signing in from anywhere else (another country, Cloudflare's unknown `XX`, Tor `T1`) is flagged (`ops.auth.country_changed`) and must confirm with the second step (authenticator app or recovery code), even staff who otherwise sign in with the password alone.
  - Staff with no second step set up are refused from there (`ops.auth.country_needs_second_factor`): setting one up at that moment would let a password alone through.
  - A session lives only where it was confirmed: the device gate ends it (`CountryChanged`) when the device is used from another country than its own and the session was not confirmed there. Signing in without the device and adding its cookie later, or travelling during a session, both end in a fresh sign-in with the second step.
  - A request with no country header at all (not through Cloudflare: a LAN, localhost) tells nothing and is never flagged.
  - A device registered before countries were kept takes the first real country it is used from, from a fully signed-in session only (never before the second factor).
  - The sign-in checks the device without rotating its secret.
- To check on staging: that `CF-IPCountry` arrives from Cloudflare's addresses (otherwise countries stay empty and no device is ever flagged).

## R-7 Licences under the company (owner, 2026-10-21, 18:50 and 19:08)

- **Control:** no Devices menu or list of its own. Each tenant's page lists its licences: every licence bound to a device or free, then the devices requested and those that ended; licence, device ID, name, type, status, date. The name opens the device's page (revoke; releases over the allowance decided there). Control does not show who authorised or revoked a device.
- **Operations:** a *Licences* tab in the company (`/staff/devices`) with the same list and all the details: device ID, name, type, browser, status (requested, bound, release requested, revoked...), registered date, authorised by (name and date). The name opens the device's page, where a request is approved or denied and a bound licence released, each with a fresh code. The Devices menu entry is gone.
- **A decrease of licences (downgrade), as in V3:** it waits for the renewal, and it can only be asked for once the devices in use fit the lower number (release licences first; a release over the allowance waits for Vixly staff). While it is scheduled, the lower number already caps new approvals, so the renewal always fits. The Licences tab says from when the lower number applies.

## R-8 One customers' mailbox per company (owner, 2026-10-21, 23:00)

- "Each company set a warehouse as customer mailbox and all customer's shipment goes to this same warehouse." Customers cannot choose another warehouse.
- **The warehouse page** has *Customers' mailbox*; one per company (choosing it on one warehouse takes it from the other). It must be an active warehouse that receives and accepts deliveries; one that stops receiving stops being the mailbox. Until a company chooses, its oldest such warehouse is the mailbox (the update names it).
- **The portal** shows only that address; pre-alerts, new shipments and missing-package reports go there; a pre-alert can join only a shipment leaving from it.
- **Counter drop-offs** still happen at any receiving warehouse (owner's answer, 2026-10-21).
- **Fixed in package 94:** the global script of package 93 stopped on a database made before the mailbox (*Invalid column name 'IsMailbox'*): its first-mailbox step named the new column in the same batch. The step now runs on its own; the script upgrades every earlier database again. A database where the package 93 script failed simply runs the package 94 script.

## Owner decision recorded, not built yet (2026-10-21, 18:31)

R-6, a device refused for an out-of-country sign-in: "if the laptop is stolen, the user might have his email open also. I would block sign-in until company system admin unblock them. At sign in I would notify admin that a device is block because of out of country sign in and need his attention." An emailed code is therefore not a way through. To build on the owner's go-ahead: the refusal blocks sign-in from that device until a company administrator unblocks it, and administrators are notified at once that the device is blocked for an out-of-country sign-in and needs their attention.

## Verification

| Check | Result |
|---|---|
| Bundle list built at the desk, confirmed by scans, closed only when nothing is missing | `BundleList_BuiltAtTheDesk_ConfirmedByScans_ClosesOnlyWhenNothingIsMissing` passes |
| Portal pricing (regular and own, never another customer's) | `Customer_EstimatesRates_SeesTheirQuotes_AndOnlyTheirOwnPrices` passes |
| Public pricing: setting and published only; menu, page and sitemap agree | `Pricing_OnThePublicWebsite_OnlyWhatTheCompanyPublishes` passes |
| Work email at another company opens no customer account; an invitation never accepted blocks nobody | `SignUp_WithAWorkEmailOfAnyCompany_OpensNoAccount` passes |
| Customer email refused when the staff invitation is accepted; the company learns nothing | `StaffInvite_WithACustomerEmailOfAnyCompany_IsRefusedWhenAccepted` passes |
| The Owner is not refused | `Invitation_OfTheOwner_IsAcceptedEvenWithACustomerEmail` passes |
| Cross-tenant reads allow-listed | `IgnoreQueryFilters_OnlyInTheReviewedReads` passes |
| Device type, browser, licence, authorised by; flagged sign-in in the activity; session ended in a third country | `Device_TypeAndBrowser_ThenASignInFromAnotherCountry_IsFlaggedInTheActivity` passes |
| Sign-in without the device, then its cookie from another country | `SignIn_WithoutTheDevice_ThenItsCookieFromAnotherCountry_EndsTheSession` passes |
| Second factor asked abroad; refused without one; at home as usual | `Device_InAnotherCountry_IsFlagged_AndConfirmedWithTheCode`, `Device_InAnotherCountry_WithoutASecondFactor_IsRefused` pass |
| Country rule (another, unknown, Tor; no header; no country of its own) | `SignInPlace_ChangesCountry_OnlyAgainstTheDevicesOwn` passes |
| Platform keeps origin, approver, first country | `Enrol_KeepsWhereItCameFrom_AndApproval_KeepsWho` passes |
| Contracts additive only | golden shapes updated (Enrol, link, verify bodies; check and device info) |
| Database | one global script; upgrade from every earlier package and an empty database, twice |
