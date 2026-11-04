---
slug: support-calls
title: Support calls and outside accounts
summary: Let a call centre answer customers about one shipment at a time, without seeing their details.
section: setting-up
order: 70
permissions: [ops.support.lookup, ops.staff.edit, ops.customers.manage]
conditions: []
routes: [/staff/support, /staff/support/requests]
related: [branches-and-warehouses, ticket-desk]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Support/SupportPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Staff/StaffMemberPage.razor
updated: 2026-10-29
review: ok
---

A support agent answers callers about one shipment or package at a time. They never see a customer list, a full address, a full phone number or an email, and they change nothing: they open a call ticket for {brandName}'s staff.

## Set up an agent
1. Invite the agent like any staff member and give them the **Support agent** role for the whole company.
2. Open the person and, under **Outside account**, tick **This is an outside account**.
3. Choose the **End date** of the contract, the **Working days**, and the times **From** and **To** in your company's time zone. Select **Save**.

An outside account can only use **Support calls**, only on its days and hours, and until its end date. Outside them it cannot sign in, and an open session stops working.

## Take a call
1. Open **Support calls** and type the caller's full phone number. Select **Find the caller**. You see only their first name, last initial, code and whether they are in good standing.
2. Ask for the tracking number: your company's, or the carrier's (Amazon, UPS, USPS...) for a package. Select **Open**.
3. It opens only if the shipment or package is the caller's, as sender or recipient. You see where it is, holds, the destination and the amount due.
4. When the caller asks for a change, write it and select **Open a call ticket**.

A caller with no account, such as a recipient, gets the status only, with your company's tracking number.

## What happens next
The agent chooses the ticket's topic; the department that handles that topic sees it in **Tickets** (see *The ticket desk*). Every phone and tracking number lookup is recorded in the audit log, and an agent can make at most 60 an hour.
