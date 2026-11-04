---
slug: find-a-company
title: Find a company
summary: Search the company directory and identify the right account.
section: companies
order: 10
permissions: [platform.tenants.view]
conditions: []
routes: [/tenants]
related: [company-page, registrations]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantList.razor
updated: 2026-10-23
review: ok
---

Use the company directory when you need to find an existing Vixly company account. Check the company name, code and current status before opening a record. Similar names can belong to different companies.

The search in the top bar also leads to this directory. A registration that has not yet produced a company record belongs in the registration list, so an empty company search does not by itself mean that sign-up never started.

## Steps
1. Open **Tenants** from the navigation.
2. Enter the company name or code in **Search name, code, address, email or phone**. Use a distinctive part of the name when the full spelling is uncertain.
3. Narrow the list with **Status** or **Tag** when those details are known. Choose **Apply** to apply your filters.
4. Open the matching company name. Confirm its identity on the company page before continuing with a support or billing task.

## What happens next
The company page brings together its profile and the sections available to your account. Searching or opening the record changes nothing. If no result matches, check the filters and the registration list before starting another sign-up.