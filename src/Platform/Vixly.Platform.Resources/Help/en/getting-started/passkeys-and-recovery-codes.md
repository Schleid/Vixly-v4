---
slug: passkeys-and-recovery-codes
title: Passkeys and recovery codes
summary: Manage your own passkeys and keep replacement recovery codes safe.
section: getting-started
order: 30
permissions: []
conditions: []
routes: []
related: [sign-in-and-second-factor]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Account/MySecondFactors.razor
  - src/Platform/Vixly.Platform.Web/Features/Account/MySecondFactorsPresenter.cs
updated: 2026-10-23
review: ok
---

A passkey lets you use the sign-in method supported by your device or security key. Recovery codes help you regain access when your usual second factor is unavailable. Both belong to your own account.

Prepare a safe place for recovery codes before replacing them. Never send them in a support message or leave them where another person can read them. A colleague helping with access should not ask you to disclose a code.

## Steps
1. Open **My account** and find **Second factors**. Review whether your authenticator is enabled and how many unused recovery codes remain.
2. To add a passkey, enter **Name for the new passkey (for example, Work laptop)** and choose **Add a passkey**. Complete the browser or device prompt. Give the passkey a name you can recognize later.
3. To remove a listed passkey, choose **Remove** beside it. Complete identity confirmation if requested.
4. Choose **Get new recovery codes** to replace your recovery codes. Store the new codes securely before choosing **I have saved them**.

## What happens next
Replacement codes supersede the previous set. Keep the new set rather than relying on an older copy. If your browser does not support passkeys, read the message shown and continue using your available sign-in methods.