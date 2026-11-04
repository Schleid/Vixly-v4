---
slug: setup-checklist
title: Set up your company step by step
summary: Use the workspace checklist to see which initial setup tasks remain.
section: setting-up
order: 10
permissions: [ops.settings.edit]
conditions: []
routes: []
related: [company-settings, branches-and-warehouses, shipping-services, workspace-at-a-glance]
sources:
  - src/Operations/Vixly.Operations.Web/Components/SignIn/WorkspacePage.razor
  - src/Operations/Vixly.Operations.Application/UseCases/Settings/GetOperationsSetupChecklist/GetOperationsSetupChecklistUseCase.cs
updated: 2026-10-25
review: ok
---

The setup checklist shows the initial tasks that remain for {brandName}. It appears on the workspace home for the owner or an administrator while setup work is outstanding. It is an overview, not a replacement for checking each configuration screen.

The checklist reflects recorded company information. A completed item means that its initial requirement was found; it does not certify that every price, contact or business choice has been reviewed.

## Steps
1. Open **Home** and find **Finish setting up** when it is shown.
2. Read the outstanding items. The checklist covers company settings, the first branch, a receiving warehouse, shipping services, the published website and an additional staff member.
3. Use an unfinished item’s link to open its configuration page. Review the information already there before making an appropriate change.
4. Return to the workspace after saving and check the updated checklist. Use the help link beside its heading for this overview.

## What happens next
Completed items show a check mark. When every initial item is complete, the checklist no longer needs to appear on the home page. You can still reach configuration through the normal navigation. Completing the list does not receive a package, take a payment or approve a device; those are separate tasks.