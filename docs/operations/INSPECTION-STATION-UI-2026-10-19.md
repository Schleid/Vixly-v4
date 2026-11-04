# Inspection station presentation — 2026-10-19

The initial scan page now has a dedicated inspection workspace: a parcel/scale/scanner vector sketch, a clear empty-state heading, an aligned scan field and Open action, keyboard guidance and a three-part condition/measurements/photos guide. Once a parcel is open, the introduction and guide disappear and the lookup remains compact above the existing inspection form.

The GET action, code field name, autofocus, scanner/keyboard input attributes and loaded-parcel bindings are preserved. The existing owner Terms notice and enforcement are unchanged. No queue counts, live scanner connection claims, fake barcodes or measurements are introduced. All new copy has English, French and Spanish resources; French and Spanish are marked needs-review. Colors use existing semantic tokens. The stylesheet is separately scoped and versioned.

## Verification

- Operations.Web compiled through the isolated Razor preview with current workspace dependencies.
- Existing UI architecture tests: 479 passed (current source checks using the existing test assembly).
- Actual empty and loaded components rendered at 1440px and 375px; no document-level horizontal overflow measured.
- French and Spanish empty states fit 375px. Scan input is 56px high; the Open action is 56px on desktop and 48px on mobile.
- Original GET action `/staff/inspection` verified in the rendered form. No business form was submitted.
- Screenshots and measurements are in the task's inspection-station-20261019 visualization directory. Data is synthetic, with no database or production session used.

The preview does not validate physical scanner hardware, photo upload persistence, real authorization, the owner accepting Terms, or end-to-end database workflows. Those behaviors remain with the existing backend. Integration still depends on the separate Operations backend baseline currently present in the shared workspace; no deployment is included.