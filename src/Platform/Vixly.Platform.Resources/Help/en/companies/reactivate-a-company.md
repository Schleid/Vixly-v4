---
slug: reactivate-a-company
title: Reactivate a company
summary: Reactivate a company after reviewing its account and current status.
section: companies
order: 42
permissions: [platform.tenants.reactivate]
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

Reactivate a suspended or deactivated company when the reason for keeping it inactive has been resolved. Review its latest history and record why access can be restored before confirming the action.

## Steps
1. Open the correct company and review **Overview**. Confirm its identity and current status against the request you are handling.
2. Find **Access and status** and choose **Reactivate**. Only moves available from the current status are offered on this panel.
3. Enter **Reason**. Keep the explanation factual and suitable for the internal account history.
4. Choose **Reactivate** in the confirmation form. Confirm your identity when requested and wait for the result before leaving the page.

## What happens next
The company becomes active and an open suspension is closed. Reactivation does not erase the history of the earlier decision. Check any subscription or billing question in its own section.

If the action is unavailable, reload the company and review its latest status. Use **Cancel** to leave an unfinished confirmation without applying the move.