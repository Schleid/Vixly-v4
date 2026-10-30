# Public contact form illustration — 2026-10-17

Owner: Frontend/UI. User requested imagery/sketches alongside the proposed Public form redesign.

- Added original editorial warehouse/scanning/dispatch artwork, optimized as a 293 KB WebP. Prompt and provenance are recorded in wwwroot/images/README.md.
- Contact-only CSS composes the existing title, introduction, sketch, direct email and form into two balanced columns. The company and message span the form width; name/email and phone/operation pair on desktop. Mobile uses one column and a shorter illustration.
- The artwork is decorative, contains no required text and does not sit behind fields. Existing localized HTML supplies the meaning. Error messages retain their role and receive their own grid area.
- This delivery changes only CSS and assets. No form markup, model, validation, bot-check, antiforgery, submission handler, backend or resource file is changed. Registration and payment forms retain their existing presentation.

Verification: Public build succeeds with zero warnings/errors. Browser checked the actual HTTPS contact page at its default desktop width, English/French at 375px and Spanish at 768px, with no document overflow at the tested widths. Inputs remain labelled, native controls remain visible, the initial-page Turnstile test widget renders, and the submit button remains readable. No lead was submitted. Screenshot saved with this chat's visualization artifacts. Temporary preview hosts were shut down.

Publication: review branch only; production requires merge and deployment. Existing untracked Public application/backend delivery files are excluded from this styling commit.
