using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.ChangeOperationsTicketSettings;

// =========================================================
// FILE: ChangeOperationsTicketSettingsResponse.cs
//
// PURPOSE:
// How long a ticket may wait before the managers are told: not taken after so many hours, not resolved after so many days (owner, 2026-10-29, ticket desk section 5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsTicketSettingsResponse
{
    private ChangeOperationsTicketSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsTicketSettingsResponse Success()
    {
        return new ChangeOperationsTicketSettingsResponse { Succeeded = true };
    }

    public static ChangeOperationsTicketSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsTicketSettingsResponse { Error = error };
    }
}
