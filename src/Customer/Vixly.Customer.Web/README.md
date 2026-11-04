# Vixly.Customer.Web

Customer portal (Blazor Server), its own process behind Edge at `{tenant-host}/customer/**`. One cookie scheme: customer, `Path=/customer`.

| | |
|---|---|
| Owner | Customer team |
| Build stage | 3 |
| May reference | Application, Infrastructure (composition root only), its Resources and Contracts, Shared.UI, Shared.Help, Shared.Hosting and Shared.Localization |
| Must not reference | Operations or Platform internals; staff routes. |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md). Do not add code here until this project's stage opens.

Customer help Version 2 starts with mailbox usage, package announcements and shipment creation under `/customer/help`. Operations verifies customer sessions; guides use company branding and fictional localized page-only screenshots. See `docs/help/HELP-3-V2-VERIFICATION.md` for the priority scope and release gates.
