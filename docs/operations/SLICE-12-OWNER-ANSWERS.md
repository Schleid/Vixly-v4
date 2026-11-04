# Slice 12: handheld and driver apps - owner answers

Recorded 2026-10-23. Replaces the plan's line "Handheld and driver apps: .NET MAUI scan and driver apps on the outbox with an offline queue (O-4, O-5)".

| # | Topic | Owner's answer |
|---|---|---|
| H-1 | App type | **Browser apps first.** Extend `/staff/scan` and the driver page. The only installed app is a small print helper (H-6) |
| H-2 | Devices | Android phones, rugged handhelds (Zebra, Honeywell), iPhones, Windows PCs and tablets |
| H-3 | Driver at the door | Route and stops, proof of delivery, cash on delivery, live location |
| H-4 | Offline | **The whole route.** Every stop works without a signal and is sent when back in coverage |
| H-5 | Live location | **Company and customer.** Staff see drivers on a map. The customer sees "the driver is N stops away" and, at the last stop, the map; in the portal, or through a private link in the "out for delivery" text that works only that day. Only while the route runs |
| H-6 | Printing | **Portable Bluetooth label printers, through a small native print helper.** Name: "Vixly Print" for ordinary companies; a white-label company gets its own copy under its own brand |
| H-7 | Handheld jobs | Receiving and moving parcels, plus inspection, bundle and load, destination receiving and pickup, and a **basic stock count**. Full counts and the exceptions queue stay in the warehouse management add-on |
| H-8 | Sign-in on shared handhelds | The handheld is licensed once, like a computer. Each person signs in **with their staff badge (scan) or a 4-digit PIN** (either one, not both). The authenticator is asked once a day. A lost badge is revoked and a new one printed |
| H-9 | Licences | **Every handheld and phone counts** as a licensed device of the plan |
| H-10 | Partner agents | Use the same driver app, limited to the stops given to them |
| H-11 | Timing | **Before go-live** |

## Also decided

- **Hostnames:** production stays on `vixly.ltd`. ADR-0010 (Amendment 1), the Global Standard's hostname table and the production settings use `vixly.ltd`.
- **After Slice 12, in this order:**
  1. Control's invitation error says "user saved, email failed".
  2. The public plans page lists SMS/WhatsApp, carrier data and API features.
  3. Maintenance windows (outline agreed 2026-10-22, Control backlog section 1).
  4. Hostnames to `vixly.ltd`.
  5. Platform job records: a run history and a "Jobs" page, as Operations has.
