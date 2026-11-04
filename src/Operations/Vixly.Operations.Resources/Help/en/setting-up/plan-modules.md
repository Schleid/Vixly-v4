---
slug: plan-modules
title: What your plan includes, and entering customers
summary: Some plans leave out parts of the workspace, such as the customer portal or insurance. See what changes, and how to enter a customer yourself.
section: setting-up
order: 107
permissions: [ops.customers.manage]
conditions: []
routes: [/staff/customers/new]
related: [plan-shipments, company-settings]
sources:
  - src/Operations/Vixly.Operations.Application/Plans/OperationsPlanModules.cs
  - src/Operations/Vixly.Operations.Web/Components/Customers/CustomerNewPage.razor
updated: 2026-10-30
review: ok
---

The {brandName} plan decides which parts of the workspace you use. When a part is not in your plan, its pages are not in the menu, and the counter does not offer it.

## Parts a plan may leave out
- *Customer portal:* customers cannot create an account, sign in or request shipments online. Your website shows no sign-in or account buttons; it keeps the tracking page.
- **Insurance**: no insurance products at the counter, and no insured shipments.
- **Credit accounts**: no bill-my-account at the cashier.
- **Supplies**: no supplies sold at the counter.
- **Home delivery**: shipments are picked up at a branch.
- *Support desk and tickets.*
- *Bills and profit.*

## Enter a customer yourself
Without the customer portal, customers do not register themselves. Staff who manage customers add them:
1. Open **Customers** and choose **New customer**.
2. Enter the first and last name, the company name if any, the email, the phone and the language.
3. Choose **Create the customer**. The customer gets a customer number and is active at once.

## Good to know
- The email is required and must not belong to another customer: notices and receipts go there.
- When your plan includes the customer portal, customers register themselves and **New customer** is not shown.
- Your plan is shown on **Billing**. To use a part your plan leaves out, ask for a larger plan.
