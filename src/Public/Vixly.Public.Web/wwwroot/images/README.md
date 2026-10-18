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

## Registration workspace sketch — 2026-10-17

`registration-workspace-sketch.webp` is a distinct registration illustration, generated with built-in imagegen and encoded as WebP (261,582 bytes). It is decorative; all instructions remain localized HTML.

Final generation prompt:

> Use case: stylized-concept. Asset type: original editorial illustration for Vixly logistics company registration page, an onboarding companion artwork. Scene: an adult female logistics company owner seated at a clean desk, setting up her business on an open laptop, a small plant and two neatly stacked shipping cartons beside the desk; behind her, a light isometric cutaway warehouse with organized racks and one small delivery truck; a cobalt dotted route connects three tiny location pins in the air above the warehouse. Distinct from a contact-sales warehouse loading scene: focus on the seated business owner and workspace setup. Style: refined architect's pencil and ink sketch, premium editorial illustration for enterprise SaaS, delicate slate-gray linework, sparse cobalt blue watercolor accents, light muted ochre cardboard, warm ivory paper background fading gently at edges. Composition: portrait 4:5, uncluttered, generous breathing room, owner/laptop centered in lower middle, warehouse lightly sketched above, attractive at 400px wide. No text, no numbers, no letters, no logos, no watermark, no form UI, no fake readable interface, no childish cartoon, no gradient background.

## Illustrated homepage and Features journey — 2026-10-17

Eight original sketches generated with the built-in imagegen tool. Optimized WebP copies total less than 1 MB; originals remain in Codex generated-images storage. All are decorative and contain no required UI text.

Each final prompt uses this exact shared prefix and suffix, with the corresponding subject inserted between them:

Prefix: Use case: stylized-concept. Asset type: original premium editorial illustration for Vixly logistics SaaS public website. Subject:

Suffix: Style: refined architectural pencil and ink sketch on very pale warm ivory paper, precise delicate slate linework, selective enterprise cobalt-blue watercolor accents and restrained ochre cardboard, natural adult human proportions, professional and welcoming, high clarity at small display sizes, generous empty breathing room, edges fade gently into clean paper. Match one cohesive visual family across website illustrations. No text, letters, numbers, readable labels, logos, watermark, decorative UI, dashboard screenshots, heavy dark backgrounds, gradients, childish cartoon, or photoreal stock-photo rendering.

- `sketch-shipments.webp`: A freight coordinator checking a shipment waybill on a clipboard beside three labeled-looking but unreadable cartons, an air cargo pallet and a distant small aircraft silhouette. Clear shipment intake and tracking theme. Landscape 4:3.
- `sketch-parcels.webp`: Close editorial scene of a handheld barcode scanner pointed at the barcode stripes on a parcel on a short conveyor; a worker's hands only, neat neighboring cartons. Strong focus on the physical scan, no words or screen UI. Landscape 4:3.
- `sketch-dispatch.webp`: Three delivery vans at organized loading bays, workers assembling grouped cartons on two roll cages for dispatch, a light cobalt dotted route curving toward a distant road. Clear bundle assembly and route departure. Landscape 4:3.
- `sketch-warehouse.webp`: Isometric warehouse inventory scene: organized shelving, varied cartons, a worker checking stock with a handheld device, another moving a pallet jack. Clear physical storage and inventory counts without text. Landscape 4:3.
- `sketch-billing.webp`: A logistics finance specialist's desk: hands reviewing an invoice sheet with fine illegible line marks, calculator, bank card, two small shipping cartons at edge; subtle cobalt ledger-book cover. Clear invoicing and payment reconciliation, no fake interface. Landscape 4:3.
- `sketch-organization.webp`: Three separate miniature warehouse-office buildings connected to their own blue key-shaped access tokens, clearly separated private spaces; two staff figures at a doorway, subtle shield outline integrated behind the central building. Thoughtful architectural editorial metaphor for company isolation and role-based access. No giant padlock cliché. Landscape 4:3.
- `sketch-customers.webp`: A courier handing a parcel to a business customer at a small shop entrance, customer checking shipment arrival on a phone with screen turned away. Human customer visibility and delivery theme. Landscape 4:3.
- `sketch-hero.webp`: A sophisticated wide isometric cutaway of one connected logistics operation: warehouse shelving at center, worker scanning a carton at intake, consolidated parcels moving on a pallet toward a loading bay, delivery van leaving onto a curved cobalt route, a small office finance desk at upper right. One coherent airy architectural composition, no segmented panels, clear end-to-end operations. Landscape 3:2.
