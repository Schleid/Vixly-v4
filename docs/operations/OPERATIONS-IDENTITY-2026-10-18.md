# Operations visual identity

The signed-in shell is completed and superseded by the [Operations workbench coverage record](WORKBENCH-UI-COVERAGE-2026-10-18.md). Its grouped sidebar, responsive directory tables and shared form/access styling are the current implementation.

The staff-entry portion is superseded by the owner-approved [blue-panel staff login](APPROVED-STAFF-LOGIN-2026-10-18.md). The workbench and storefront refinements below remain applicable.

Operations is a tenant logistics application with its own staff entry and workbench. This revision supersedes the security-themed staff entry described in STOREFRONT-STAFF-STYLING-2026-10-18.md. Control is unchanged.

The staff entry uses an open white form beside original receiving photography, charcoal actions and an amber orientation marker. Mobile places the form before the photograph. Recovery and MFA inherit the same restrained visual language. Existing workspace links and setup checks receive a task-launcher layout; no shipment metrics or unfinished business features are fabricated.

The existing editor shell uses a light horizontal task rail, current-page underline, skip link, compact responsive forms, contained table scrolling and visible keyboard focus. The storefront keeps published tenant branding and image settings, with clearer tracking, section rules and country labels. The decorative route overlay was removed.

Authentication, authorization, antiforgery, session behavior, endpoints and business logic are retained. Existing English, French and Spanish resources are reused. Authentication currently receives the company name rather than a tenant brand palette; no cross-plane branding access was introduced.

## Asset provenance

`staff-receiving.webp` is an original image generated with the built-in imagegen tool in generation mode, then resized to 960 × 1200 and encoded as WebP. It illustrates generic warehouse receiving, not a particular tenant facility. The copied Control security image is removed from Operations.

Final generation prompt:

Use case: photorealistic-natural. Asset type: original portrait editorial photograph for a logistics company's STAFF sign-in page, visibly different from a software platform/security dashboard. Scene: authentic orderly freight warehouse at start of shift, one professional warehouse worker in slate-gray workwear with a muted amber safety vest, seen in three-quarter profile, using a handheld barcode scanner on a plain cardboard parcel at a receiving bench. Stacked parcels and tall industrial shelving recede into a softly lit background. Warm morning light from high windows, concrete floor, realistic shipping labels with no readable text, purposeful work atmosphere. Composition tall portrait 4:5, worker and scanner in the middle-right, generous quiet space in upper-left for a small tenant-name label to be rendered in HTML, details readable in a 600px-wide crop. Palette warm stone, charcoal, cardboard kraft, small amber highlights; restrained editorial photography, not glossy corporate stock. No logos, brand names, readable text or numbers, shields, locks, laptops, floating interfaces, cyber-security motifs, blue gradients, watermarks, staged handshake or courier handoff. Photograph illustrates a generic receiving workflow, not a specific tenant facility.

## Validation

- Actual Razor components reviewed in a local isolated HTTPS fixture, using sample company and location data, without production authentication or database writes.
- Desktop entry, storefront, workspace and staff editor reviewed. French mobile entry at 375px and Spanish tablet entry at 768px have no horizontal overflow; mobile editor navigation scrolls within its rail. Keyboard focus advances from email to password with a visible outline.
- Operations UI build passed with zero warnings/errors. All 438 focused UI architecture tests passed.
- After the backend team supplied its missing seeder file, the final Operations web-test run built successfully: 10 passed, 31 database-dependent checks skipped, zero failures.
- The Operations backend/project baseline remains separate uncommitted work owned by its team. It must be integrated before this review branch can build independently from a clean checkout. This styling revision does not stage that baseline or deploy the application.
