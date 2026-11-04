using Vixly.Operations.Domain.Inspection;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.GetOperationsInspectionSetup;

// =========================================================
// FILE: GetOperationsInspectionSetupResponse.cs
//
// PURPOSE:
// The company's restricted-items list, inspection rules and random share (Slice 3C design section 3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsInspectionSetupResponse
{
    private GetOperationsInspectionSetupResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsRestrictedItem> Items { get; private init; } = Array.Empty<OperationsRestrictedItem>();

    public IReadOnlyList<OperationsInspectionRule> Rules { get; private init; } = Array.Empty<OperationsInspectionRule>();

    public int Percent { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public static GetOperationsInspectionSetupResponse Success(IReadOnlyList<OperationsRestrictedItem> items, IReadOnlyList<OperationsInspectionRule> rules, int percent, string currencyCode)
    {
        return new GetOperationsInspectionSetupResponse { Succeeded = true, Items = items, Rules = rules, Percent = percent, CurrencyCode = currencyCode };
    }

    public static GetOperationsInspectionSetupResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsInspectionSetupResponse { Error = error };
    }
}
