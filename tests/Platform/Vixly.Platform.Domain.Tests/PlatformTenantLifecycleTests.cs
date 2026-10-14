using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;
using static Vixly.Platform.Domain.Tests.TenantTestData;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformTenantLifecycleTests.cs
//
// PURPOSE:
// Proves the tenant lifecycle (D-T1): every move from every
// reachable status, allowed or refused.
//
// RESPONSIBILITIES:
// - Each allowed move lands on the right status and returns its
//   history event
// - Each refused move names the from and to statuses
// - Suspension is opened and resolved; ban is terminal
//
// IMPORTANT NOTES:
// - Pending activation is entered in Slice 5; it is covered there
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformTenantLifecycleTests
{
    public static TheoryData<PlatformTenantStatus, string, PlatformTenantStatus?> Moves()
    {
        TheoryData<PlatformTenantStatus, string, PlatformTenantStatus?> data = new();
        PlatformTenantStatus A = PlatformTenantStatus.Active;
        PlatformTenantStatus S = PlatformTenantStatus.Suspended;
        PlatformTenantStatus X = PlatformTenantStatus.Deactivated;
        PlatformTenantStatus B = PlatformTenantStatus.Banned;
        PlatformTenantStatus D = PlatformTenantStatus.Draft;

        data.Add(D, "activate", A); data.Add(D, "suspend", null); data.Add(D, "reactivate", null); data.Add(D, "deactivate", X); data.Add(D, "ban", B);
        data.Add(A, "activate", null); data.Add(A, "suspend", S); data.Add(A, "reactivate", null); data.Add(A, "deactivate", X); data.Add(A, "ban", B);
        data.Add(S, "activate", null); data.Add(S, "suspend", null); data.Add(S, "reactivate", A); data.Add(S, "deactivate", X); data.Add(S, "ban", B);
        data.Add(X, "activate", null); data.Add(X, "suspend", null); data.Add(X, "reactivate", A); data.Add(X, "deactivate", null); data.Add(X, "ban", B);
        data.Add(B, "activate", null); data.Add(B, "suspend", null); data.Add(B, "reactivate", null); data.Add(B, "deactivate", null); data.Add(B, "ban", null);

        return data;
    }

    [Theory]
    [MemberData(nameof(Moves))]
    public void Move_IsAllowedOnlyFromItsStatuses(PlatformTenantStatus from, string move, PlatformTenantStatus? expected)
    {
        PlatformTenant tenant = InStatus(from);
        DateTimeOffset later = Now.AddMinutes(5);

        Func<PlatformTenantLifecycleEvent> act = move switch
        {
            "activate" => () => tenant.Activate(Actor, later),
            "suspend" => () => tenant.Suspend(PlatformSuspensionCategory.Security, "Breach", Actor, later),
            "reactivate" => () => tenant.Reactivate("Resolved", Actor, later),
            "deactivate" => () => tenant.Deactivate("Closed", Actor, later),
            _ => () => tenant.Ban("Fraud", Actor, later)
        };

        if (expected is null)
        {
            DomainException refused = Assert.Throws<DomainException>(() => act());
            Assert.Contains(refused.Error.Code, new[] { "platform.tenants.move_not_allowed", "platform.tenants.already_banned" });
            Assert.Equal(from, tenant.Status);
            return;
        }

        PlatformTenantLifecycleEvent recorded = act();

        Assert.Equal(expected, tenant.Status);
        Assert.Equal(later, tenant.StatusChangedUtc);
        Assert.Equal(from, recorded.FromStatus);
        Assert.Equal(expected, recorded.ToStatus);
        Assert.Equal(tenant.Id, recorded.PlatformTenantId);
        Assert.Equal(Actor, recorded.ActorUserId);
    }

    [Fact]
    public void RefusedMove_NamesBothStatuses()
    {
        PlatformTenant tenant = Draft();

        AssertError(
            PlatformTenantErrors.MoveNotAllowed(PlatformTenantStatus.Draft, PlatformTenantStatus.Suspended),
            () => tenant.Suspend(PlatformSuspensionCategory.Administrative, "x", Actor, Now));
    }

    [Fact]
    public void Suspension_IsOpened_AndResolvedByReactivation()
    {
        PlatformTenant tenant = InStatus(PlatformTenantStatus.Active);

        PlatformTenantLifecycleEvent suspended = tenant.Suspend(PlatformSuspensionCategory.FraudInvestigation, " Chargebacks ", Actor, Now);

        Assert.Equal(PlatformSuspensionCategory.FraudInvestigation, suspended.Category);
        Assert.Equal("Chargebacks", suspended.Reason);
        Assert.NotNull(tenant.OpenSuspension);

        tenant.Reactivate("Cleared", Actor, Now.AddDays(1));

        PlatformTenantSuspension closed = Assert.Single(tenant.Suspensions);
        Assert.False(closed.IsOpen);
        Assert.Equal("Cleared", closed.ResolutionReason);
        Assert.Null(tenant.OpenSuspension);
    }

    [Fact]
    public void Reactivating_ANeverActiveTenant_ReturnsItToDraft()
    {
        PlatformTenant tenant = Draft();
        tenant.Deactivate("Lead lost", Actor, Now);

        tenant.Reactivate("Lead back", Actor, Now);

        Assert.Equal(PlatformTenantStatus.Draft, tenant.Status);
        Assert.False(tenant.HasBeenActive);
    }

    [Fact]
    public void Activation_KeepsTheFirstActivationTime()
    {
        PlatformTenant tenant = InStatus(PlatformTenantStatus.Suspended);

        tenant.Reactivate("ok", Actor, Now.AddDays(3));

        Assert.Equal(Now, tenant.ActivatedUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void ReasonedMoves_NeedAReason(string? reason)
    {
        PlatformTenant tenant = InStatus(PlatformTenantStatus.Active);

        AssertError(PlatformTenantErrors.ReasonRequired, () => tenant.Suspend(PlatformSuspensionCategory.Security, reason, Actor, Now));
        AssertError(PlatformTenantErrors.ReasonRequired, () => tenant.Deactivate(reason, Actor, Now));
        AssertError(PlatformTenantErrors.ReasonRequired, () => tenant.Ban(reason, Actor, Now));
        Assert.Equal(PlatformTenantStatus.Active, tenant.Status);
    }

    [Fact]
    public void Ban_IsTerminal()
    {
        PlatformTenant tenant = InStatus(PlatformTenantStatus.Banned);

        AssertError(PlatformTenantErrors.AlreadyBanned, () => tenant.Ban("again", Actor, Now));
        AssertError(
            PlatformTenantErrors.MoveNotAllowed(PlatformTenantStatus.Banned, PlatformTenantStatus.Draft),
            () => tenant.Reactivate("undo", Actor, Now));
    }

    [Fact]
    public void UnknownCategory_IsRefused()
    {
        PlatformTenant tenant = InStatus(PlatformTenantStatus.Active);

        AssertError(PlatformTenantErrors.SuspensionCategoryInvalid, () => tenant.Suspend((PlatformSuspensionCategory)99, "x", Actor, Now));
    }

    [Fact]
    public void Moves_NeedAnActor()
    {
        PlatformTenant tenant = Draft();

        AssertError(PlatformTenantErrors.ActorRequired, () => tenant.Activate(Guid.Empty, Now));
    }
}
