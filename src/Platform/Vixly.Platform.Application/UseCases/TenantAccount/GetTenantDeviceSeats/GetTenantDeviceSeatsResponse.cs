using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetTenantDeviceSeats;

// =========================================================
// FILE: GetTenantDeviceSeatsResponse.cs
//
// PURPOSE:
// Result of GetTenantDeviceSeats.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetTenantDeviceSeatsResponse
{
    private GetTenantDeviceSeatsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public int Seats { get; private init; }

    public int InUse { get; private init; }

    public int? ScheduledSeats { get; private init; }

    public DateTimeOffset? ScheduledUtc { get; private init; }

    public static GetTenantDeviceSeatsResponse Success(int seats, int inUse, int? scheduledSeats, DateTimeOffset? scheduledUtc)
    {
        return new GetTenantDeviceSeatsResponse { Succeeded = true, Seats = seats, InUse = inUse, ScheduledSeats = scheduledSeats, ScheduledUtc = scheduledUtc };
    }

    public static GetTenantDeviceSeatsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetTenantDeviceSeatsResponse { Error = error };
    }
}
