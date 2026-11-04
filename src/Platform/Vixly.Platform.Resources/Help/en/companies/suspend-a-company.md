---
slug: suspend-a-company
title: Suspend a company
summary: Suspend a company after reviewing its account and current status.
section: companies
order: 41
permissions: [platform.tenants.suspend]
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

Suspend an active company when its account must be paused. Choose the appropriate suspension category and provide an internal reason. The category and the internal explanation serve different purposes.

## Steps
1. Open the correct company and review **Overview**. Confirm its identity and current status against the request you are handling.
2. Find **Access and status** and choose **Suspend**. Only moves available from the current status are offered on this panel.
3. Enter **Reason**. Select **Category** as well. Keep the explanation factual and suitable for the internal account history.
4. Choose **Suspend** in the confirmation form. Confirm your identity when requested and wait for the result before leaving the page.

## What happens next
The company becomes suspended. Its notice states the suspension category and how to contact Vixly, without disclosing the internal reason. Check the recorded status before answering the support request.

If the action is unavailable, reload the company and review its latest status. Use **Cancel** to leave an unfinished confirmation without applying the move.