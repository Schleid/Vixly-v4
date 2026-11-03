---
slug: ticket-desk
title: The ticket desk
summary: Every ticket goes to the department that handles its topic, someone takes it, and managers are told when it waits too long.
section: setting-up
order: 71
permissions: [ops.parcels.view, ops.invoices.view, ops.customers.manage, ops.settings.edit]
conditions: []
routes: [/staff/tickets/]
related: [support-calls]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Tickets/TicketsPage.razor
  - src/Operations/Vixly.Operations.Web/Components/Tickets/TicketPage.razor
updated: 2026-10-29
review: ok
---

**Tickets** gathers the customers' missing-package tickets and the tickets support agents open during calls. Each ticket has a topic, and only the department of that topic sees it.

## Who sees what
- **Missing or wrong package:** the warehouse's staff (who can view parcels there).
- **Billing and payment:** Finance (who can view invoices or take payments).
- **Pickup, delivery, recipient:** customer service, and the shipment staff of the branch.
- **Damage or claim:** customer service and insurance.
- **Other:** customer service.

The Owner and the Administrators see every ticket.

## Handle a ticket
1. Open **Tickets**. Filter by state (**New**, **Taken**, **Escalated**...), topic, warehouse or **Taken by me**.
2. Open a ticket and select **I'm on it**, or choose a colleague and select **Give**.
3. A ticket in the wrong department: choose another topic and select **Move**.
4. Answer, or **Add a note** on a support call's ticket (the customer is not emailed about those), and resolve it.

## When managers are told
Under **When managers are told**, set after how many hours nobody took a ticket, and after how many days it is still not resolved. The managers of the ticket's department get one email at each step; the second also goes to the Owner. Escalated tickets show **Escalated** and come first.
