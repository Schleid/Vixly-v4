---
slug: company-page
title: Read a company's page
summary: Understand the company overview and its available sections.
section: companies
order: 20
permissions: [platform.tenants.view]
conditions: []
routes: [/tenants/{TenantId:guid}]
related: [find-a-company, edit-company-details]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantSummary.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantCompanyFacts.razor
updated: 2026-10-23
review: ok
---

A company's page is the starting point for support work on its Vixly account. It brings the overview, contacts and account history together. Other sections, such as subscription, billing or licensing, appear according to your access.

Use the section links to move around the page instead of searching through every panel. A company status, a subscription status and an invoice balance describe different things. Read the relevant section before deciding what needs attention.

## Steps
1. Find the company in **Tenants** and open its name.
2. Review **Overview**. Compare the company code and identity with the request you are handling.
3. Consult **Company** and **Contacts** to understand the profile and recorded contacts.
4. Read **Internal notes** and **History** for the context available to you. Follow a section link only when it concerns your task.

## What happens next
Opening or reading a company does not change its status. Each available action has its own confirmation and result. Keep internal notes separate from messages intended for the company. If another section is unavailable, ask the colleague responsible for that work to handle it with their own account.