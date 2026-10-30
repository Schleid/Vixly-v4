# Compact Operations layouts — 2026-10-20

Addresses the customer record's unused column, stacked configuration sections, oversized controls and shipping-service inner scrollbar reported in the owner's screenshots.

- Customer sections become peer tabs: details, incoming packages, status and credit. Existing permission conditions determine which panels exist. Details use a compact two-pair definition grid; credit/status inputs remain short.
- Locations separates branches, warehouses and contacts into tabs, with compact inline record actions and content-sized status pills.
- Inspection configuration separates random share, rules and restricted items. Holds separates the live hold queue from unclaimed/configuration and approval sections; live holds is the default.
- Shipment details, parcels and pre-alert sections become tabs. The small service-change action remains available outside the tabs.
- Workbench headings lose decorative art and excess height. Desktop inputs/buttons are compact; numeric/code fields use appropriate short widths, while coarse-pointer controls retain 44px targets. Approved inspection scan controls remain large.
- Shipping-service day ranges are horizontal; labels/divisors have bounded widths. Both fixed-height scroll containers around the service editor are removed. Address fields use three columns at wide desktop widths.

`ops-tabs.js` progressively enhances existing markup. Without JavaScript every section is visible. With it, tabs provide ARIA relationships, arrow/Home/End navigation, URL-fragment selection/deep links and invalid-field reveal. Labels reuse existing localized headings. Forms, field names/actions, antiforgery and authorization conditions are unchanged. No backend code changes.

Verification: UI architecture source checks 537 passed using the existing test assembly. Operations.Web build passed with zero warnings/errors; eight synthetic tab-behavior checks passed. Browser visual verification is not claimed: browser observation was rejected by the URL policy; no workaround was attempted. Existing shared-tree backend changes remain separate from frontend staging.