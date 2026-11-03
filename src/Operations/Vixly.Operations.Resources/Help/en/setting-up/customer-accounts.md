---
slug: customer-accounts
title: Suspend, reactivate, close or ban a customer
summary: When a customer breaks your terms or asks to leave: what each action does, who can do it, and what the customer is told.
section: setting-up
order: 109
permissions: [ops.customers.manage]
conditions: []
routes: [/staff/customers]
related: [plan-modules]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Customers/CustomerStatusPanel.razor
  - src/Operations/Vixly.Operations.Domain/Customers/OperationsCustomer.cs
updated: 2026-10-31
review: ok
---

Open the customer from **Customers**. At the bottom of the page, choose the change and give a reason. The reason stays in the customer's history; the customer receives an email without it.

## The actions
- **Suspend**: the customer cannot sign in or send new shipments. Optionally choose **Reactivate automatically on**: the account becomes active again by itself that day. You can suspend again to change the day.
- **Reactivate**: ends a suspension, or reopens a closed account.
- **Close the account**: for a customer who leaves or asks to close. It can be reopened later.
- **Ban (breaks our terms, permanent)**: the account can never be reopened, and the customer's email and phone number cannot open a new account with your company.

Only the Owner and administrators can ban a customer or reopen a closed account. Those who manage customers can suspend, reactivate and close.

## What happens to work in progress
Packages already received and shipments already paid keep moving, and can still be picked up. A customer who is not active cannot be billed on credit: they pay before their goods are released.

## Good to know
- The customer's page also lists their latest shipments, with where each one stands.
- Credit accounts are opened and changed from **Credit**, not from the customer's page.
