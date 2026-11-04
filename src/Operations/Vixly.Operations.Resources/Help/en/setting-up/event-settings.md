---
slug: event-settings
title: Event causes and notifications
summary: Event settings give your company a consistent set of reasons for delays and exceptions, together with notification choices.
section: setting-up
order: 180
permissions: [ops.settings.view]
conditions: []
routes: [/staff/settings/events]
related: [company-settings, pickup-settings]
sources:
  - src/Operations/Vixly.Operations.Web/Components/Events/EventSettingsPage.razor
updated: 2026-10-25
review: ok
---

Event settings give {brandName} a consistent set of reasons for delays and exceptions, together with notification choices. Review the customer-facing wording before changing a cause. Configuring a reason makes it available for later work; it does not record that an event happened to any shipment.

## Steps
1. Open **Events settings** and review the existing notification choices.
2. Check **Email customers about delays and exceptions (one email per shipment)** and **Email customers when a shipment is ready for pickup** where editing is available.
3. Review the standard causes listed for each kind of event. Keep the causes your company intends staff to use selected.
4. Under **The company’s own causes**, review existing custom causes and their active status.
5. If **Add a cause** is available, select the event kind, complete the localized wording fields and add the cause. Use **Save** for changes to an existing form.

## What happens next
Reopen the page and check the saved causes and notification choices. Review all language fields so customers receive meaningful wording. A cause can be taken out of use without recording a new operational event. Staff must still select the appropriate reason when recording a real delay or exception.