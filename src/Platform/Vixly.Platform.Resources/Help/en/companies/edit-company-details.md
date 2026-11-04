---
slug: edit-company-details
title: Edit company contacts and account settings
summary: Update contacts, language, time zone and account management details.
section: companies
order: 30
permissions: [platform.tenants.edit]
conditions: []
routes: []
related: [company-page, company-change-requests]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantProfileEditor.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantContactsEditor.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantAccountManagement.razor
updated: 2026-10-23
review: ok
---

Use the company page to maintain its operational contact information and account settings. These changes are different from approving a change to the company's legal identity or address.

The legal company details are locked on this page. Review a submitted company change request for that work. Internal notes and tax settings also have their own actions; editing a contact does not update either of them.

## Steps
1. Open the correct company and find **Company**. Choose **Edit settings** to change the language or time zone.
2. Review the values and choose **Save**. Use **Cancel** if you do not want to keep the edit.
3. In **Contacts**, choose **Edit**. Update the contact name, email or optional telephone number in the appropriate contact group, then save.
4. In **Account management**, choose **Edit** to change **Account manager** or **Tags**. Check the entries before saving.

## What happens next
The page reloads its company details after a successful change. Confirm the displayed result before moving to another section. Use factual, current contact details supplied for that company. Do not put confidential support explanations into a contact field or a tag.