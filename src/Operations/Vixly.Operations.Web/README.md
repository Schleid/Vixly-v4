# Vixly.Operations.Web

Staff / warehouse application (Blazor Server). One cookie scheme: staff. No customer routes.

| | |
|---|---|
| Owner | Operations team |
| Build stage | 2 |
| May reference | Application, Infrastructure (composition root only), its Resources, Shared.UI, Shared.Help, Shared.Hosting and Shared.Localization (Global Standard section 3) |
| Must not reference | Customer plane; policy-scheme routers; business logic in components. |

Governed by the [Global Standard](../../../docs/global/VIXLY-GLOBAL-STANDARD.md). Do not add code here until this project's stage opens.

Operations help Version 1 starts at `/staff/help` under the existing staff/device/access gates. Text articles belong to Resources; the request-owned reader reuses GetOperationsMenu and known plan features. See `docs/help/HELP-4A-VERIFICATION.md` for the initial scope and remaining work. No screenshots are part of this version.
