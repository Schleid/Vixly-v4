# Help stage 1 (Public): verification

| | |
|---|---|
| Date | 2026-10-22 |
| Pull request | Pending; local implementation on the existing workspace branch |
| Signed by owner | Pending |
| Foundation | H0 approved by Jude after its 14,744 architecture, 52 engine and 12 UI checks passed |

## Articles

All 16 slugs are present in English, French and Spanish (48 embedded files). Body lengths are 186–243 whitespace-delimited words after inserting the exact resource labels. Public articles have no permissions, conditions or company tokens.

| Slug | Title (en) | Permissions / conditions | Routes | fr | es |
|---|---|---|---|---|---|---|
| what-vixly-does | What Vixly does for your company | Public; no conditions | /en, /fr, /es, /en/features, /fr/features, /es/features | needs-review | needs-review |
| choose-a-plan | Choose a plan | Public; no conditions | /en/plans, /fr/plans, /es/plans | needs-review | needs-review |
| licences-and-devices | Licences and devices | Public; no conditions |  | needs-review | needs-review |
| sign-up-overview | Sign up your company | Public; no conditions | /en/register, /fr/register, /es/register | needs-review | needs-review |
| company-details | Enter your company details | Public; no conditions |  | needs-review | needs-review |
| confirm-your-email | Confirm your email | Public; no conditions | /en/register/email, /fr/register/email, /es/register/email, /register/verify | needs-review | needs-review |
| choose-your-plan-at-sign-up | Choose your plan and licences | Public; no conditions | /en/register/plan, /fr/register/plan, /es/register/plan | needs-review | needs-review |
| pay-to-start | Pay your first invoice | Public; no conditions | /en/register/pay, /fr/register/pay, /es/register/pay, /register/payment | needs-review | needs-review |
| after-sign-up | When your workspace is ready | Public; no conditions | /en/register/done, /fr/register/done, /es/register/done | needs-review | needs-review |
| sign-up-by-invitation | Sign up with an invitation or offer | Public; no conditions |  | needs-review | needs-review |
| sign-up-questions | Sign-up questions | Public; no conditions |  | needs-review | needs-review |
| pay-an-invoice-link | Pay an invoice from an email | Public; no conditions | /pay/{Token} | needs-review | needs-review |
| save-a-card | Save a card for automatic payments | Public; no conditions | /cards/{Token} | needs-review | needs-review |
| pay-by-zelle | Pay by Zelle | Public; no conditions |  | needs-review | needs-review |
| terms-and-privacy | Terms and privacy | Public; no conditions | /en/legal, /fr/legal, /es/legal | needs-review | needs-review |
| contact-sales | Contact Vixly | Public; no conditions | /en/contact, /fr/contact, /es/contact | needs-review | needs-review |

## Corrections to the stage document

- Current routes are three explicit language routes rather than one language-parameter template. Exact route strings are listed in front matter. Email payment/card routes keep the code's Token parameter spelling.
- A trial or a first period with nothing due skips payment and moves to account setup. The overview does not promise five mandatory steps.
- An invitation can open sign-up while general registration is closed. Help remains public in either case.
- The Zelle form asks for the bank confirmation code, sender, amount and date. The articles describe those fields, not an additional invented payment reference.
- Public.Web has no Application project or reader permissions. Its presenter uses the public reader directly, with no authorization use case or cross-plane dependency.
- Account sign-in and device setup remain workspace responsibilities. Public explains the email handoff without duplicating Operations instructions.
- Finding published/legal documents is covered here; accepting updated terms on an existing account stays in that account's help.

## Findings for the product

- RegistrationProgress says that the payment page will show the next step once confirmed, but does not install a refresh timer. This help does not promise automatic polling on that page.
- The catalogue localizes more feature labels than PlanDisplay.Known currently displays. The articles refer to features actually listed.
- No product bugs or missing labels were repaired in H1.

## Checks

| Check | Result |
|---|---|
| Public build | Passed; zero warnings and errors |
| Public HTTP/presenter/content tests | 46 passed, including exact translated labels and source paths |
| Publication checks against actual endpoint data source | Passed in the Public HTTP suite |
| Architecture checks | 14,771 passed in the full H1 run; 20 focused help/format checks passed after final additions |
| Browser, all help articles en/fr/es | All 48 opened; search, language switching, contextual links and mobile layout checked |
| Every article followed against development demo workflows | Pending; article reachability alone does not prove a workflow walkthrough |
| French/Spanish review | 32 article drafts and 32 new chrome resource values marked needs-review; native review blocks release |
| Product behaviour | Changes limited to help routes, search, navigation, metadata and sitemap |

The Development API readiness check passed. Live plans, registration, legal and contact screens were inspected. The full registration/email/payment/card/Zelle demo walkthrough remains outstanding; the automated and browser checks above do not substitute for that stage requirement.

## Gate

Owner authorized proceeding to H2 (Control) on 2026-10-23. This opens Control under that explicit instruction; it does not certify the outstanding Public workflow walkthrough or native translation review.
