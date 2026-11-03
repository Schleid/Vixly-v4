---
slug: change-company-status
title: Activate a company
summary: Activate a company after reviewing its account and current status.
section: companies
order: 40
permissions: [platform.tenants.activate]
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

Activate a company when it is ready to become active in Vixly. The action is offered for a draft company or one awaiting activation. Confirm the intended account before changing its status.

## Steps
1. Open the correct company and review **Overview**. Confirm its identity and current status against the request you are handling.
2. Find **Access and status** and choose **Activate**. Only moves available from the current status are offered on this panel.
3. Review the confirmation shown before continuing. Activation has no internal-reason field on this form.
4. Choose **Activate** in the confirmation form. Wait for the result before leaving the page.

## What happens next
The company moves to active after the action succeeds. Activation is a company status change; it does not prove that every device or staff account has completed setup.

If the action is unavailable, reload the company and review its latest status. Use **Cancel** to leave an unfinished confirmation without applying the move.