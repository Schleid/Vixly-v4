# Public logistics photography

Owner: Frontend/UI. Created 2026-10-16 using the built-in imagegen tool. These are generated illustrative scenes, not customer facilities. No third-party images or remote image hosts are used.

- `warehouse-scanning.webp`: decorative home hero; parcel scanning inside a warehouse.
- `morning-dispatch.webp`: decorative home workflow banner; dispatch vans at loading bays.

Optimized to WebP without cropping or resizing: approximately 113 KB and 175 KB. Generated originals are retained in the Codex generated-images directory. All page copy and actions remain HTML. Images are CSS decorations with no accessibility announcement; they convey no information absent from the adjacent content. No backend logic or localization keys changed.

## Generation prompts

Hero: Premium editorial photograph for Vixly logistics software. Modern warehouse dispatch, organized parcels on a conveyor, shelving and loading bays in depth, professional in a blue jacket using a barcode scanner, distant unbranded van. Natural daylight, warm cardboard, slate and restrained cobalt accents. Centered composition for square crops. No text, readable labels, logos, watermarks or software screens.

Workflow: Premium realistic editorial wide photograph for Vixly logistics software. Distribution center in early morning, three unbranded white vans along warehouse loading bays, blue structural trim, warm sunlight, parcels on a loading cart and a staff member beside it. Authentic commercial environment and architectural photography. Panoramic composition. No readable text, logos, watermarks or software overlays.

## Checks

Generated source images visually inspected. CSS diff whitespace checks pass. Hero changes to one column at the existing tablet breakpoint and both images use taller crops on mobile. Live browser layout verification remains pending; no preview process was started or left running for this change.

## Section-specific Features images

Corrected the broad adjacent-sibling selector: it selected every feature with a following feature, repeating the inventory photo. The new selectors match each section's own heading ID. Verified seven unique assignments and seven existing assets:

- Shipments: `feature-shipments.webp`, pallet preparation at a freight terminal.
- Parcels: `warehouse-scanning.webp`, barcode scanning.
- Dispatch: `morning-dispatch.webp`, loading bays and vans.
- Warehouse and inventory: `warehouse-inventory.webp`, stock checking among storage shelves.
- Billing: `feature-billing.webp`, invoice review with calculator and paperwork.
- Customers: `feature-customers.webp`, customer receiving a delivery and checking a phone.
- Organization: `feature-organization.webp`, branch supervisors coordinating operations.

Four new images generated with built-in imagegen and visually inspected. Shared prompt: premium editorial logistics photography, natural daylight, navy/cobalt accents, authentic environment, landscape crop, no logos, readable text, watermark or interface overlays. Scene prompts are the descriptions above. Optimized WebP assets live in Public wwwroot/images; originals remain in Codex generated-images storage. No markup, backend or resource changes. CSS and asset-reference checks pass. Live browser verification remains pending.

## Contact sales illustration — 2026-10-17

`contact-logistics-sketch.webp` is original artwork generated with the built-in imagegen tool, then encoded as WebP (293,062 bytes). It decorates the Contact sales form only; no information depends on the image. The original PNG remains in Codex generated-images storage.

Final generation prompt:

> Use case: stylized-concept. Asset type: finished editorial illustration for the left column of Vixly logistics SaaS Contact Sales web form. Primary request: refined architect's isometric logistics sketch, warehouse shelving with cartons, a small handheld barcode scanner in foreground, two adult logistics colleagues discussing a clipboard, and a delivery van at loading bay, linked by a subtle cobalt dotted route. Style: sophisticated airy editorial ink and pencil line drawing, precise fine slate strokes, restrained enterprise cobalt blue wash accents, warm pale ivory paper, slight hand-drawn texture, premium B2B product illustration, inviting human scale, accurate readable silhouettes. Composition: landscape 4:3, one coherent scene with generous breathing room around edges, designed to sit beside a real HTML form at 450px wide, no surrounding UI or form. Palette: off-white, slate gray, deep cobalt, small muted amber carton accents. No text, no letters, no labels, no logos, no watermark, no gradients, no dark background, no stock-photo look, no childish cartoon, no dashboard mockup.
