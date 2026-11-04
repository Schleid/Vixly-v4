using Vixly.Operations.Application.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Settings.GetOperationsSetupChecklist;

// =========================================================
// FILE: GetOperationsSetupChecklistResponse.cs
//
// PURPOSE:
// Works out which setup steps the tenant has done (Slice 1E, design section 3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsSetupChecklistResponse
{
    private GetOperationsSetupChecklistResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsSetupItem> Items { get; private init; } = Array.Empty<OperationsSetupItem>();

    public static GetOperationsSetupChecklistResponse Success(IReadOnlyList<OperationsSetupItem> items)
    {
        return new GetOperationsSetupChecklistResponse { Succeeded = true, Items = items };
    }

    public static GetOperationsSetupChecklistResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsSetupChecklistResponse { Error = error };
    }
}
