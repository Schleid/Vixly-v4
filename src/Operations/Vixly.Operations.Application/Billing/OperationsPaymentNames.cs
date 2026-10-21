using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsPaymentNames.cs
//
// PURPOSE:
// The payment methods a company offers, with its own names for its other methods (Slice 5F, C-1).
//
// RESPONSIBILITIES:
// - List the accepted methods in order
// - Give the company's name of an "other" method
//
// IMPORTANT NOTES:
// - The standard methods are named by text keys on the pages; "other" methods by the company
// =========================================================
public static class OperationsPaymentNames
{
    public static IReadOnlyList<OperationsPaymentMethod> Accepted(OperationsTenantSettings settings) =>
        Enum.GetValues<OperationsPaymentMethod>().Where((settings ?? throw new ArgumentNullException(nameof(settings))).Accepts).ToList();

    public static IReadOnlyDictionary<OperationsPaymentMethod, string> Others(OperationsTenantSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Dictionary<OperationsPaymentMethod, string> names = new();

        foreach (OperationsPaymentMethod method in new[] { OperationsPaymentMethod.Other1, OperationsPaymentMethod.Other2, OperationsPaymentMethod.Other3 })
        {
            if (settings.OtherName(method) is { } name)
            {
                names[method] = name;
            }
        }

        return names;
    }
}
