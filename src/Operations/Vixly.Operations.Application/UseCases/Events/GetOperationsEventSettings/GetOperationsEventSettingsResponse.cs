using Vixly.Operations.Domain.Events;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Events.GetOperationsEventSettings;

// =========================================================
// FILE: GetOperationsEventSettingsResponse.cs
//
// PURPOSE:
// Settings → Events (Slice 6C, E-3 and E-5): the emails, the standard causes and the company's own.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsEventSettingsResponse
{
    private GetOperationsEventSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool EventEmails { get; private init; }

    // Slice 7: the ready-for-pickup email.
    public bool PickupEmails { get; private init; } = true;

    public IReadOnlyList<string> DisabledCauses { get; private init; } = Array.Empty<string>();

    public IReadOnlyList<OperationsEventReason> Reasons { get; private init; } = Array.Empty<OperationsEventReason>();

    public bool CanEdit { get; private init; }

    public static GetOperationsEventSettingsResponse Success(bool eventEmails, IReadOnlyList<string> disabledCauses, IReadOnlyList<OperationsEventReason> reasons, bool canEdit,
        bool pickupEmails = true)
    {
        return new GetOperationsEventSettingsResponse
        {
            Succeeded = true, EventEmails = eventEmails, DisabledCauses = disabledCauses, Reasons = reasons, CanEdit = canEdit, PickupEmails = pickupEmails
        };
    }

    public static GetOperationsEventSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsEventSettingsResponse { Error = error };
    }
}
