---
slug: company-change-requests
title: Review company change requests
summary: Compare proposed legal details with the current company and supporting proof.
section: companies
order: 60
permissions: [platform.tenants.review_changes]
conditions: []
routes: [/tenants/changes, /tenants/changes/{RequestId:guid}]
related: [company-page, edit-company-details]
sources:
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantChangeView.razor
  - src/Platform/Vixly.Platform.Web/Features/Tenants/TenantChangesOverview.razor
updated: 2026-10-23
review: ok
---

Company change requests let you review proposed changes to legal company details. Compare the request with the existing record and its supporting proof before making a decision.

A rejection explanation is different from a private working note. Read the field's purpose before entering text, and write a clear explanation the company can act on. Do not approve a request merely because its proposed spelling looks plausible.

## Steps
1. Open **Company changes**, select the relevant status and apply the filter. Open the request you intend to review.
2. Compare **Current details** with **Requested details**. Check the company identity and the submitted proof linked on the request.
3. To accept the proposed change, enter **Note (optional)** if needed and choose **Approve**. Complete the identity confirmation when requested.
4. To decline it instead, enter **Why it is rejected (sent to the tenant)** and choose **Reject**. Explain what needs correction rather than leaving an unclear refusal.

## What happens next
Read the resulting status and inspect the company details after approval. A previously decided request cannot be treated as a new pending request. If the screen reports that the record changed, reload it and review the latest information before deciding.