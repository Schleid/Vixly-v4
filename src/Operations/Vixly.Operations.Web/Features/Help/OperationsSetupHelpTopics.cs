namespace Vixly.Operations.Web.Features.Help;

// =========================================================
// FILE: OperationsSetupHelpTopics.cs
//
// PURPOSE:
// Maps checklist tasks to the setup guides published in H4a.
//
// RESPONSIBILITIES:
// - Leave later-stage tasks without a misleading article link.
//
// IMPORTANT NOTES:
// - Reader visibility is checked by OperationsHelpArticleLink.
// =========================================================
public static class OperationsSetupHelpTopics
{
    public static string ForChecklist(string code) => code switch
    {
        "settings" => "company-settings",
        "branch" or "receiving_warehouse" => "branches-and-warehouses",
        "services" => "shipping-services",
        _ => string.Empty
    };
}
