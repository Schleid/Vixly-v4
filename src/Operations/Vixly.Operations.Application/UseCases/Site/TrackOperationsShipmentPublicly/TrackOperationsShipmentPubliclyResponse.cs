using Vixly.Operations.Application.Site;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.TrackOperationsShipmentPublicly;

// =========================================================
// FILE: TrackOperationsShipmentPubliclyResponse.cs
//
// PURPOSE:
// Public tracking by number: milestones and dates only (Slice 1B, owner decision; ADR-0021).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class TrackOperationsShipmentPubliclyResponse
{
    private TrackOperationsShipmentPubliclyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsPublicMilestone> Milestones { get; private init; } = Array.Empty<OperationsPublicMilestone>();

    public static TrackOperationsShipmentPubliclyResponse Success(IReadOnlyList<OperationsPublicMilestone> milestones)
    {
        return new TrackOperationsShipmentPubliclyResponse { Succeeded = true, Milestones = milestones };
    }

    public static TrackOperationsShipmentPubliclyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new TrackOperationsShipmentPubliclyResponse { Error = error };
    }
}
