---
slug: superuser-actions
title: SuperUser responsibilities
summary: Understand exceptional account decisions reserved for SuperUser.
section: team-and-security
order: 90
permissions: [superuser]
conditions: []
routes: []
related: [company-page, sign-in-and-second-factor]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantLifecycle.razor
  - src/Platform/Vixly.Platform.Web/Features/Users/UserInviteForm.razor
  - src/Platform/Vixly.Platform.Web/Features/Users/SignInSecurityPanel.razor
  - src/Platform/Vixly.Platform.Application/Security/PlatformPermissions.cs
updated: 2026-10-23
review: ok
---

SuperUser carries exceptional responsibilities in Vixly Control. A role grant does not make an ordinary staff member a SuperUser. Use your own account and keep a clear reason for decisions that affect access.

A company ban is terminal. It is different from suspension or deactivation, which have their own help. Do not use a ban when the intended outcome is a temporary pause.

## Steps
1. Before banning a company, open its page and verify the identity, history and reason for the decision. Find **Access and status**.
2. Choose **Ban**, enter **Reason** and review the confirmation. Choose the action again and complete identity confirmation when requested.
3. When inviting staff, review **Authority level** before sending an invitation. Grant only the intended authority; choosing a role is a separate responsibility.
4. Review sign-in policy changes on their own settings panel. Read the explanation and complete identity confirmation before applying an access-policy change.

## What happens next
A banned company cannot be returned to active by the ordinary reactivation action. The internal reason belongs in account history. An invitation's authority choice affects the invited account; it does not alter another existing staff account.