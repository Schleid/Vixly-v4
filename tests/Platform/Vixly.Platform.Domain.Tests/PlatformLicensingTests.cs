using Vixly.Platform.Domain.Licensing;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformLicensingTests.cs
//
// PURPOSE:
// Proves the licensing rules of Slice 7: seats, device states, both proof methods, releases and links.
//
// RESPONSIBILITIES:
// - Seats never exceed what was bought; numbers are handed out in order
// - Approval, denial, expiry; final states stay final
// - Rotating secret with grace and reuse detection
// - Releases within the allowance at once, over it to review; lost devices stop at once
// - Links: single use, tries, voiding
//
// IMPORTANT NOTES:
// - Tests assert codes and states, never text (L-1)
// =========================================================
public sealed class PlatformLicensingTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly Guid Tenant = Guid.Parse("0199f200-0000-7000-8000-0000000000aa");

    private static readonly PlatformDeviceActor Admin = PlatformDeviceActor.Tenant("ops-user-7", "Grace Admin");

    private static readonly string HashA = new('a', 64);

    private static readonly string HashB = new('b', 64);

    private static void AssertCode(string code, Action act)
    {
        DomainException error = Assert.Throws<DomainException>(act);
        Assert.Equal(code, error.Error.Code);
    }

    private static PlatformDevice Active(PlatformTenantLicensing seats, string? secret = null)
    {
        PlatformDevice device = PlatformDevice.Enrol(Tenant, seats.NextNumber(Now), "Front desk PC", PlatformDeviceKind.Pc, secret is null ? "MFkwEwYHKoZIzj0CAQ" : null, secret, Admin, Now).Device;
        seats.Apply(device.Approve(Admin, Now), 3, Now);
        return device;
    }

    [Fact]
    public void Seats_NeverExceedWhatWasBought_AndNumbersRunInOrder()
    {
        PlatformTenantLicensing seats = PlatformTenantLicensing.Create(Tenant, Now);

        Assert.Equal("D-000001", seats.NextNumber(Now));
        seats.TakeSeat(2, Now);
        seats.TakeSeat(2, Now);
        DomainException full = Assert.Throws<DomainException>(() => seats.TakeSeat(2, Now));
        Assert.Equal("platform.licensing.no_free_seat", full.Error.Code);
        seats.ReturnSeat(Now);
        Assert.Equal(1, seats.SeatsInUse);
    }

    [Fact]
    public void Enrol_NeedsANameAndExactlyOneProof_AndFinalStatesStayFinal()
    {
        AssertCode("platform.licensing.name_invalid", () => PlatformDevice.Enrol(Tenant, "D-1", " ", PlatformDeviceKind.Pc, "key", null, Admin, Now));
        AssertCode("platform.licensing.public_key_invalid", () => PlatformDevice.Enrol(Tenant, "D-1", "PC", PlatformDeviceKind.Pc, "key", HashA, Admin, Now));
        AssertCode("platform.licensing.public_key_invalid", () => PlatformDevice.Enrol(Tenant, "D-1", "PC", PlatformDeviceKind.Pc, null, null, Admin, Now));
        AssertCode("platform.licensing.actor_required", () => PlatformDeviceActor.Tenant(null, "Grace"));

        (PlatformDevice device, PlatformDeviceEvent requested) = PlatformDevice.Enrol(Tenant, "D-1", "Scanner 2", PlatformDeviceKind.Scanner, null, HashA, Admin, Now);
        Assert.Equal(PlatformDeviceProof.Secret, device.Proof);
        Assert.Equal(PlatformDeviceEventCodes.Requested, requested.EventCode);
        Assert.False(device.HoldsSeat);

        device.Deny(PlatformDeviceReason.NotRecognized, Admin, Now);
        Assert.Equal(PlatformDeviceStatus.Denied, device.Status);
        AssertCode("platform.licensing.not_allowed_in_status", () => device.Approve(Admin, Now));
    }

    [Fact]
    public void Pending_ExpiresAfterSevenDays()
    {
        PlatformDevice device = PlatformDevice.Enrol(Tenant, "D-1", "PC", PlatformDeviceKind.Pc, "key", null, Admin, Now).Device;

        Assert.Null(device.ExpireIfDue(Now.AddDays(6)));
        Assert.NotNull(device.ExpireIfDue(Now.AddDays(7)));
        Assert.Equal(PlatformDeviceStatus.Expired, device.Status);
    }

    [Fact]
    public void Secret_RotatesAfter24Hours_OldOneWorksFiveMinutes_ThenCountsAsReused()
    {
        PlatformTenantLicensing seats = PlatformTenantLicensing.Create(Tenant, Now);
        PlatformDevice device = Active(seats, HashA);

        Assert.Equal(PlatformDeviceSecretCheck.Valid, device.CheckSecret(HashA, Now.AddHours(23)));
        Assert.Equal(PlatformDeviceSecretCheck.ValidRotate, device.CheckSecret(HashA, Now.AddHours(24)));
        Assert.Equal(PlatformDeviceSecretCheck.Invalid, device.CheckSecret(HashB, Now));

        device.RotateSecret(HashB, Now.AddHours(24));
        Assert.Equal(PlatformDeviceSecretCheck.Valid, device.CheckSecret(HashB, Now.AddHours(24)));
        Assert.Equal(PlatformDeviceSecretCheck.Valid, device.CheckSecret(HashA, Now.AddHours(24).AddMinutes(4)));
        Assert.Equal(PlatformDeviceSecretCheck.Reused, device.CheckSecret(HashA, Now.AddHours(24).AddMinutes(6)));

        PlatformDevice keyed = Active(seats);
        Assert.Equal(PlatformDeviceSecretCheck.Invalid, keyed.CheckSecret(HashA, Now));
    }

    [Fact]
    public void Release_WithinTheAllowance_IsImmediate_AndOverIt_WaitsForVixly()
    {
        PlatformTenantLicensing seats = PlatformTenantLicensing.Create(Tenant, Now);
        PlatformDevice first = Active(seats);
        PlatformDevice second = Active(seats);
        Assert.Equal(2, seats.SeatsInUse);

        PlatformDeviceChange released = first.Release(PlatformDeviceReason.Replaced, withinAllowance: true, Admin, Now);
        seats.Apply(released, 3, Now);
        Assert.Equal(PlatformDeviceStatus.Released, first.Status);
        Assert.True(first.EndedByTenant);
        Assert.Equal(1, seats.SeatsInUse);

        PlatformDeviceChange waiting = second.Release(PlatformDeviceReason.StaffLeft, withinAllowance: false, Admin, Now);
        Assert.Equal(PlatformDeviceStatus.ReleasePending, second.Status);
        Assert.True(second.HoldsSeat);
        Assert.False(waiting.SeatFreed);

        AssertCode("platform.licensing.reason_required", () => second.DecideRelease(true, " ", PlatformDeviceActor.Staff(Guid.NewGuid()), Now));
        seats.Apply(second.DecideRelease(true, "Verified with the owner", PlatformDeviceActor.Staff(Guid.NewGuid()), Now), 3, Now);
        Assert.Equal(PlatformDeviceStatus.Released, second.Status);
        Assert.Equal(0, seats.SeatsInUse);
        AssertCode("platform.licensing.reason_required", () => Active(seats).Release(PlatformDeviceReason.CredentialReused, true, Admin, Now));
    }

    [Fact]
    public void LostDevice_StopsAtOnce_ButOverTheAllowance_ItsSeatWaitsForReview()
    {
        PlatformTenantLicensing seats = PlatformTenantLicensing.Create(Tenant, Now);
        PlatformDevice device = Active(seats);

        PlatformDeviceChange lost = device.Release(PlatformDeviceReason.LostOrStolen, withinAllowance: false, Admin, Now);
        seats.Apply(lost, 3, Now);

        Assert.Equal(PlatformDeviceStatus.Revoked, device.Status);
        Assert.True(device.SeatHeld);
        Assert.True(device.HoldsSeat);
        Assert.True(device.AwaitsReview);
        Assert.Equal(1, seats.SeatsInUse);

        // Refused: the seat stays held; later, within a new allowance, the tenant frees it itself.
        device.DecideRelease(false, "Too many releases this month", PlatformDeviceActor.Staff(Guid.NewGuid()), Now);
        Assert.True(device.SeatHeld);
        seats.Apply(device.Release(PlatformDeviceReason.LostOrStolen, withinAllowance: true, Admin, Now.AddDays(31)), 3, Now);
        Assert.False(device.HoldsSeat);
        Assert.Equal(0, seats.SeatsInUse);
    }

    [Fact]
    public void Withdraw_AndRevoke_FreeOrKeepTheSeat()
    {
        PlatformTenantLicensing seats = PlatformTenantLicensing.Create(Tenant, Now);
        PlatformDevice device = Active(seats);

        device.Release(PlatformDeviceReason.Broken, false, Admin, Now);
        device.WithdrawRelease(Admin, Now);
        Assert.Equal(PlatformDeviceStatus.Active, device.Status);

        PlatformDeviceChange revoked = device.Revoke(PlatformDeviceReason.SubscriptionEnded, PlatformDeviceActor.System, Now);
        Assert.True(revoked.SeatFreed);
        Assert.False(device.EndedByTenant);
        AssertCode("platform.licensing.not_allowed_in_status", () => device.Revoke(PlatformDeviceReason.Security, PlatformDeviceActor.System, Now));
    }

    [Fact]
    public void Allowance_IsTwoOrAQuarterOfSeats()
    {
        Assert.Equal(2, PlatformLicensingLimits.AllowanceFor(3));
        Assert.Equal(2, PlatformLicensingLimits.AllowanceFor(8));
        Assert.Equal(5, PlatformLicensingLimits.AllowanceFor(20));
    }

    [Fact]
    public void Links_AreSingleUse_TriedFiveTimesAtMost_AndCanBeVoided()
    {
        PlatformDeviceLink link = PlatformDeviceLink.Create(PlatformDeviceLinkKind.OwnerRecovery, Tenant, null, HashA, Now);

        Assert.True(link.IsUsableAt(Now.AddMinutes(59)));
        Assert.False(link.IsUsableAt(Now.AddMinutes(60)));
        for (int i = 0; i < 5; i++)
        {
            Assert.True(link.Try(Now));
        }

        Assert.False(link.Try(Now));

        PlatformDeviceLink first = PlatformDeviceLink.Create(PlatformDeviceLinkKind.FirstDevice, Tenant, Guid.NewGuid(), HashB, Now);
        Assert.True(first.IsUsableAt(Now.AddHours(23)));
        first.Void(Now);
        Assert.False(first.IsUsableAt(Now));

        PlatformDeviceChallenge challenge = PlatformDeviceChallenge.Create(Guid.NewGuid(), HashA, Now);
        Assert.False(challenge.Use(Now.AddMinutes(2)));
        Assert.True(PlatformDeviceChallenge.Create(Guid.NewGuid(), HashA, Now).Use(Now.AddMinutes(1)));
    }
}
