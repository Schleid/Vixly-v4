# ADR-0005: V4 is multilingual (English, French, Spanish)

- Status: **Accepted** (owner decision, 2026-10-06)
- Date: 2026-10-06
- Deciders: Jude (owner)
- Affects: Global Standard sections 1, 2, 3, 4.2, 5.2, 5.3, new 5.5, S-9, D-4, 11, 12; every plane; Slice 0 (reworked); Platform slices 1, 2, 3, 8

## Context

V4 must be offered in English (primary), French and Spanish. This touches every screen, menu, error, email, PDF and history entry. The Global Standard and the Slice 0 code did not account for it: use cases returned English sentences (`Response.Failure(string message)`, `DomainException("...")`), which would have forced translation into the business layers and made every message untestable by language. Fixing this after Slice 1 would mean rewriting every use case.

## Decision

### 1. Languages

- Supported: `en` (primary, source of truth, fallback), `fr`, `es`. Neutral language codes; no regional variants for now.
- `LanguageCode` (a value object in `Shared.Kernel`) is the only way a language is represented in code. Adding a language needs an ADR.
- Formatting culture follows the language by default (`en` to en-US, `fr` to fr-FR, `es` to es-ES). A tenant formatting-region override may be added later without changing this rule.
- Currency is data, never inferred from language: a money value carries its ISO currency code.

### 2. No user-facing text below the presentation layer

- Domain and Application never create text meant for a person. They return and throw **errors**: a stable code plus named arguments (`Error` in `Shared.Kernel`).
- Error code format: lowercase, dot-separated, plane first: `platform.identity.email_invalid`. Kernel codes start `shared.`.
- `DomainException` and `Response.Failure(...)` take an `Error`, not a string. Tests assert codes, never sentences.
- Argument values are restricted to string, integer, decimal, bool, `DateOnly` and `DateTimeOffset`, so each can be formatted in the viewer's culture.
- APIs return a problem response with `code`, `args` and a correlation id (S-9). They do not send display text for errors. The Web projects turn codes into text.
- Logs and exception messages for operators stay in English and are never shown to users.
- History and audit records store an event code plus arguments, never a sentence (D-4). The text is produced when the history is read, in the reader's language.
- Enum labels, statuses and permission names are never stored as text. Labels come from resources keyed by the enum member.

### 3. Where text lives

- Each project that owns user-visible text owns `.resx` resources in `en`, `fr` and `es`: `Shared.UI` (its own labels and accessibility text), and one `Vixly.<Plane>.Resources` project per plane holding that plane's error texts, enum labels, email and document text.
- `Vixly.Shared.Localization` (built when the first Web or Api project needs it) holds culture and request-localization setup, the error-to-text localizer, the plural helper and the pseudo-locale.
- Web, Api and Infrastructure projects may reference their plane's Resources project and `Shared.Localization`. **Domain and Application may not reference either** (architecture test).
- Resource keys for errors equal the error code. UI strings use `<Feature>.<Element>`. Placeholders are named (`{count}`), never positional, and never built by concatenation.
- Plurals use CLDR categories per language (`one`, `many`, `other` where the language needs them) through the shared plural helper. French treats 0 and 1 as singular.

### 4. Choosing the language

Order: the user's saved preference, then the tenant default, then the browser's `Accept-Language`, then `en`.

- A user's preference is stored on their account (`PreferredLanguage` on Platform users, Operations users and Customer accounts).
- A tenant has a `DefaultLanguage`, used for new users, the Customer portal and anything before sign-in.
- One culture cookie on the tenant hostname is shared by Operations and the Customer portal (same host, `/customer` path). Edge forwards it and `Accept-Language` unchanged.
- The Public site uses language in the URL (`/en/...`, `/fr/...`, `/es/...`) with `hreflang` links, for sharing and SEO.
- Server-rendered output (emails, PDFs) uses the recipient's language, not the actor's.

### 5. Translated data

- System-owned content is translated through a translation child table per entity, for example `PlatformSubscriptionPlanTranslations (PlanId, Language, Name, Description)`. The `en` row is mandatory; other languages fall back to `en`.
- Legal documents are separate approved texts per language and version. A terms acceptance records the version and the language shown.
- Documents record their issue language (for example `PlatformInvoice.Language`) and are regenerated in that language.
- **Tenant-entered content** (landing page and customer-portal wording) can be entered per language. Fallback order: requested language, then the tenant's default language, then `en`.
- Operational data (shipments, customers, notes, names) is never translated.

### 6. Formatting, input and search

- Shared.UI primitives format dates, numbers and money by culture and parse input by culture (comma decimals in French and Spanish).
- Wire and database formats are invariant: ISO 8601, `.` decimals, UTC instants. Display uses the user's culture and the tenant time zone.
- All text columns are `nvarchar`. Searchable text uses an accent-insensitive collation so "Jose" finds "José". User-visible comparisons are never done with `ToLower()`.

### 7. Layout

French and Spanish run 20 to 40 percent longer than English. No fixed-width labels or buttons; truncation needs a tooltip. A pseudo-locale (`en-XA`: accented characters, 40 percent longer) is available in Development and in the gallery, together with a language switcher, to catch hard-coded text and overflow.

### 8. Enforcement (architecture and build tests)

1. Domain and Application reference no resource or localization assembly; `DomainException` and every `Response.Failure` take an `Error`.
2. Every `Error` declared in code has an entry in its plane's resources.
3. For every `.resx` set, `en`, `fr` and `es` have identical key sets, no empty values, identical placeholder names and the plural categories each language needs. A missing translation fails the build.
4. Razor markup contains no literal user-facing text; text comes from the localizer. A deliberate exception is marked `@* i18n-ignore *@`.
5. Machine-drafted translations may be committed with the resource comment `needs-review`. They pass the build but fail the release gate until a native reviewer clears them.

### 9. Ownership and process

- Plane teams write the `en` text for their features and are responsible for keeping `fr` and `es` complete.
- First drafts of `fr` and `es` may be machine-produced. A native reviewer per language approves before release.
- A shared logistics glossary (`docs/shared/i18n-glossary.md`) fixes the French and Spanish term for each domain word (consignee, bundle, waybill, and so on) so every team uses the same one.
- Frontend/UI owns `Shared.UI` resources and the gallery language tooling. Architecture owns `Error` and `LanguageCode` in the Kernel and the enforcement tests.

## Consequences

- Global Standard updated to v1.2 (sections listed above). The Standard remains pending the owner's verification of Slice 0.
- **Slice 0 is reworked before sign-off:** `Error`, `LanguageCode`, `DomainException(Error)`, localized-error-free gate, and the new architecture rules. No signed work is changed.
- Slice 1 adds `PreferredLanguage` to Platform users. Slice 2 adds tenant `DefaultLanguage` and landing-page translations. Slice 3 adds plan translations. Slice 8 adds per-language legal documents. Slices 4 and 5 store issue language on invoices and receipts.
- Every use case's failure paths now name an error code, so each use case has a small declared error set. This is more work per use case and makes failures testable and translatable.
- Operations and Customer plane documents inherit these rules; they are not re-decided there.
- The same resources are reusable by the MAUI Hybrid heads in V2 because the text lives in Resources projects, not in Web projects.
