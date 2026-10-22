# Registration form restyle — 2026-10-17

Owner: Frontend/UI. Correct target confirmed by the owner: registration, not Contact sales.

The actual HTTPS /en/register page was inspected before styling. Registration-only CSS now uses four numbered sections, paired desktop fields, full-width address lines/owner name/portal address and clear separators instead of nested bordered cards. A distinct original workspace-setup illustration sits beside the form and remains sticky on desktop. Mobile stacks the illustration and uses single-column fields. Offer/refusal messages retain natural document order and a stacked layout.

No registration markup, handlers, models, required attributes, consent, antiforgery, bot-check, eligibility or backend code changed. CSS applies only when the existing reg-legal field is present; it does not enable closed registration. The original 14 fields and 11 required controls remain. No registration was submitted and no Terms checkbox was accepted.

Verification: actual browser review at desktop and 375/768 widths; English, French and Spanish headings and document-overflow checks passed. Native country selection, address confirmation, Terms link/checkbox and Create account action remain visible. A full-page screenshot is saved with the chat artifacts. New CSS token references/braces and diff whitespace checked. Public build passed with zero warnings and zero errors. Temporary HTTPS host shut down.

Artwork and full generation prompt: Public wwwroot/images/README.md. Source delivery is on the UI review branch; merge and deployment remain separate.
