using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Application.UseCases.Site.TrackOperationsShipmentPublicly;

// =========================================================
// FILE: TrackOperationsShipmentPubliclyUseCase.cs
//
// PURPOSE:
// Public tracking by number: milestones and dates only (Slice 1B, owner decision; ADR-0021).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Never amounts, names, addresses, photos or notes
// - Unknown and other-tenant numbers give the same answer
// =========================================================
public sealed class TrackOperationsShipmentPubliclyUseCase
{
    public Task<TrackOperationsShipmentPubliclyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        TrackOperationsShipmentPubliclyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // The tracking core arrives with Slice 3; until then every number is "not found",
        // the same answer as for a number of another tenant.
        return Task.FromResult(TrackOperationsShipmentPubliclyResponse.Failure(OperationsCommonErrors.NotFound));
    }
}
