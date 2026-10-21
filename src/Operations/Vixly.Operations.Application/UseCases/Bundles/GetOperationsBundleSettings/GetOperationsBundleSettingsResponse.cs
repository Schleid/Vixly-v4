using Vixly.Operations.Domain.Bundles;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundleSettings;

// =========================================================
// FILE: GetOperationsBundleSettingsResponse.cs
//
// PURPOSE:
// The company's bundle settings (Slice 6A, D-1): its bundle types, the target rule and the consolidation way.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsBundleSettingsResponse
{
    private GetOperationsBundleSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<string> Types { get; private init; } = Array.Empty<string>();

    public OperationsBundleTargetRule TargetRule { get; private init; }

    public OperationsConsolidationWay Way { get; private init; }

    public bool CanEdit { get; private init; }

    public static GetOperationsBundleSettingsResponse Success(IReadOnlyList<string> types, OperationsBundleTargetRule targetRule, OperationsConsolidationWay way, bool canEdit)
    {
        return new GetOperationsBundleSettingsResponse { Succeeded = true, Types = types, TargetRule = targetRule, Way = way, CanEdit = canEdit };
    }

    public static GetOperationsBundleSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsBundleSettingsResponse { Error = error };
    }
}
