---
slug: close-a-company
title: Deactivate a company
summary: Deactivate a company after reviewing its account and current status.
section: companies
order: 43
permissions: [platform.tenants.deactivate]
conditions: []
routes: []
related: [company-page]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantLifecycle.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantMoves.cs
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantLifecyclePresenter.cs
updated: 2026-10-23
review: ok
---

Deactivate a company when its Vixly company account should no longer remain active. This action is available for draft, pending, active or suspended companies. It is different from a permanent ban.

## Steps
1. Open the correct company and review **Overview**. Confirm its identity and current status against the request you are handling.
2. Find **Access and status** and choose **Deactivate**. Only moves available from the current status are offered on this panel.
3. Enter **Reason**. Keep the explanation factual and suitable for the internal account history.
4. Choose **Deactivate** in the confirmation form. Confirm your identity when requested and wait for the result before leaving the page.

## What happens next
The company becomes deactivated and the change remains in its history. Do not describe the action as deleting the company's records. A later reactivation is a separate decision with its own confirmation.

If the action is unavailable, reload the company and review its latest status. Use **Cancel** to leave an unfinished confirmation without applying the move.