---
slug: registrations
title: Follow registrations
summary: See where a self-service registration has reached.
section: companies
order: 50
permissions: [platform.tenants.view]
conditions: []
routes: [/tenants/registrations, /tenants/registrations/{RegistrationId:guid}]
related: [find-a-company]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Registrations/RegistrationList.razor
  - src/Platform/Vixly.Platform.Web/Features/Registrations/RegistrationSummary.razor
updated: 2026-10-23
review: ok
---

The registration list shows companies that started self-service sign-up. Use it to understand which step a company has reached before asking the owner to start again.

A registration and an established company account are different records. Some registrations are still waiting for email confirmation or payment. A finished registration may link to the resulting company. Read the current status rather than assuming that every entry is ready to use its workspace.

## Steps
1. Open **Registrations** and find the registration relevant to the request.
2. Open its company name and review **Registration**. Compare the company address name and owner details with the request.
3. Read the dates for **Started**, **Last link sent** and **Email verified**. These help distinguish a sent message from a completed confirmation.
4. Check **Consent** and the current status. When a company link is present, use **Open the tenant** to inspect the established account.

## What happens next
Viewing a registration does not resend an email, cancel sign-up or confirm a payment. Those are separate actions. Keep the owner on the existing sign-up journey when it is still valid, and use the recorded stage to explain what remains outstanding.