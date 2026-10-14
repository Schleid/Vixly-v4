using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantMoves.cs
//
// PURPOSE:
// Which lifecycle moves to offer for a status and an actor.
//
// RESPONSIBILITIES:
// - Mirror the D-T1 table so the console offers only possible
//   moves
//
// IMPORTANT NOTES:
// - Display only: the use case and the domain check again
// - A refused move still shows its error code
// =========================================================
public static class TenantMoves
{
    public static IReadOnlyList<TenantMove> Offered(PlatformTenantStatus status, PlatformTenantAbilities abilities)
    {
        ArgumentNullException.ThrowIfNull(abilities);

        List<TenantMove> moves = new();

        if (abilities.CanActivate && status is PlatformTenantStatus.Draft or PlatformTenantStatus.PendingActivation)
        {
            moves.Add(TenantMove.Activate);
        }

        if (abilities.CanSuspend && status == PlatformTenantStatus.Active)
        {
            moves.Add(TenantMove.Suspend);
        }

        if (abilities.CanReactivate && status is PlatformTenantStatus.Suspended or PlatformTenantStatus.Deactivated)
        {
            moves.Add(TenantMove.Reactivate);
        }

        if (abilities.CanDeactivate && status is not (PlatformTenantStatus.Deactivated or PlatformTenantStatus.Banned))
        {
            moves.Add(TenantMove.Deactivate);
        }

        if (abilities.CanBan && status != PlatformTenantStatus.Banned)
        {
            moves.Add(TenantMove.Ban);
        }

        return moves;
    }

    public static string Key(TenantMove move)
    {
        return "ui.tenants.move." + move.ToString().ToLowerInvariant();
    }
}
