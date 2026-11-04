# Full Operations and customer UI audit — 2026-10-18

This supersedes the previous spot-check completion claim in WORKBENCH-UI-COVERAGE-2026-10-18.md. That pass missed the MFA recovery-link selector and the separate customer portal.

## Presentation changes

- Access steps have a bounded 640px composition, cobalt security header and a decorative authenticator/shield illustration. Recovery links no longer select the 80rem workspace layout. The approved staff login is preserved.
- Customer and staff records use a summary rail and a separate workflow column. Mailbox addresses have their own structured panel. Restricted actions have an amber boundary and retain their original permission gates.
- Directory create/invite forms open progressively from native details. The existing shortcut opens the panel and focuses its first field.
- Locations have separate branch, warehouse and contact areas. Device records have group counts derived from the existing collection. Billing has a subscription summary and scrollable invoice tables. Role permissions remain grouped. Company changes use a responsive field grid.
- Website content fields are grouped in expandable introduction, services, steps and FAQ sections. All field names, values and the existing single save action remain intact.
- Customer.Web has an independent tenant-branded account shell, decorative account/browser sketch, registration layout, mailbox tickets and profile/password columns. No production Web-to-Web reference was added.

## Browser evidence

An external, disposable Razor preview renders the actual page components with synthetic data. All 53 page components were captured at 1440 × 1000 and 375 × 812. No document-level horizontal overflow was measured. Wide operational tables intentionally scroll within named table regions. Captures and DOM measurements are in the task's full-screen-audit visualization directory, not shipped with the application.

Additional checks: customer creation opens and focuses firstName; mobile navigation opens as a dialog with inert main content and Escape restores the toggle; MFA input retains its original pattern and is 68px high; French MFA and Spanish registration error layouts fit 375px. Authenticator QR and manual key in the fixture are explicitly synthetic, never usable credentials. Status pages are visually rendered without the cascading request because their HTTP status assignment is a host concern; this does not verify production status policy.

The fixture contains both populated records and empty collections. This is not an exhaustive test of every permission, error, or data combination. Existing server validation, authorization, form actions, antiforgery fields, session policy and customer bot checks are retained. No business endpoint was submitted during visual review.

| Page component | Desktop | Mobile | Layout result |
|---|---|---|---|
| Vixly.Customer.Web.Components.Pages.NotAvailablePage | 1440px | 375px | No page overflow |
| Vixly.Customer.Web.Components.Pages.NotFoundPage | 1440px | 375px | No page overflow |
| Vixly.Customer.Web.Components.Pages.UnavailablePage | 1440px | 375px | No page overflow |
| Vixly.Customer.Web.Components.Portal.ForgotPasswordPage | 1440px | 375px | No page overflow |
| Vixly.Customer.Web.Components.Portal.HomePage | 1440px | 375px | No page overflow |
| Vixly.Customer.Web.Components.Portal.MessagePage | 1440px | 375px | No page overflow |
| Vixly.Customer.Web.Components.Portal.ProfilePage | 1440px | 375px | No page overflow |
| Vixly.Customer.Web.Components.Portal.ResetPasswordPage | 1440px | 375px | No page overflow |
| Vixly.Customer.Web.Components.Portal.SignInPage | 1440px | 375px | No page overflow |
| Vixly.Customer.Web.Components.Portal.SignUpPage | 1440px | 375px | No page overflow |
| Vixly.Customer.Web.Components.Portal.VerifyPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Customers.CustomerListPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Customers.CustomerPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Editor.BranchFormPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Editor.ContactFormPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Editor.DevicesPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Editor.EditorMessagePage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Editor.LegalPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Editor.LocationsPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Editor.SiteEditorPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Editor.WarehouseFormPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Pages.NotAvailablePage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Pages.NotFoundPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Pages.UnavailablePage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Settings.AuditPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Settings.BillingPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Settings.CompanyPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Settings.SettingsPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Settings.ShippingServicesPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.AccessLimitedPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.DeviceEnrolPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.DeviceLinkPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.DeviceWaitingPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.EnrolPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.ForgotPasswordPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.InvitationPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.RecoveryCodesPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.RecoveryPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.ResetPasswordPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.SecondFactorResetPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.SetupHomePage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.SignInPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.VerifyPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.SignIn.WorkspacePage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Site.SiteHomePage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Site.SiteLocationsPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Site.SiteTrackPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Staff.AccountPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Staff.ConfirmPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Staff.RoleFormPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Staff.RolesPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Staff.StaffListPage | 1440px | 375px | No page overflow |
| Vixly.Operations.Web.Components.Staff.StaffMemberPage | 1440px | 375px | No page overflow |
## Validation and integration limits

- Operations.Web existing tests: 15 passed, 34 database-dependent tests skipped.
- Customer.Web existing tests: 39 passed.
- UI architecture checks: 456 passed on the final rerun.
- Both web projects compiled through the preview/test builds.

The UI branch still depends on the separate Operations/Customer backend baseline currently present in the shared workspace. This change includes presentation components and assets only, and does not commit those backend projects, endpoints, use cases or database changes. A clean-checkout integrated build and the skipped database HTTP tests remain dependent on that baseline being integrated. No deployment or main-branch merge is implied.