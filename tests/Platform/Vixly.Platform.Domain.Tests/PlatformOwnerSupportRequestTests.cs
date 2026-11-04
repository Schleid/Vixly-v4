using Vixly.Platform.Domain.Licensing;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformOwnerSupportRequestTests.cs
//
// PURPOSE:
// Proves the rules of owner support requests (D-O13).
//
// RESPONSIBILITIES:
// - Reason and identity check are required
// - A second-factor reset needs a different approver
// - Recovery starts approved; only an approved request completes
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class PlatformOwnerSupportRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Alice = Guid.NewGuid();

    private static readonly Guid Bob = Guid.NewGuid();

    [Fact]
    public void ReasonAndIdentityCheck_AreRequired()
    {
        Assert.Equal("platform.licensing.support_reason_required",
            Assert.Throws<DomainException>(() => PlatformOwnerSupportRequest.RequestSecondFactorReset(Tenant, " ", "Called back", Alice, Now)).Error.Code);
        Assert.Equal("platform.licensing.identity_check_required",
            Assert.Throws<DomainException>(() => PlatformOwnerSupportRequest.StartDeviceRecovery(Tenant, "Lost phone", null, null, Alice, Now)).Error.Code);
        Assert.Equal("platform.licensing.identity_check_required",
            Assert.Throws<DomainException>(() => PlatformOwnerSupportRequest.StartDeviceRecovery(Tenant, "Lost phone", new string('x', 501), null, Alice, Now)).Error.Code);
    }

    [Fact]
    public void SecondFactorReset_NeedsADifferentApprover_AndIsDecidedOnce()
    {
        PlatformOwnerSupportRequest request = PlatformOwnerSupportRequest.RequestSecondFactorReset(Tenant, "Lost phone and codes", "Called back the owner phone on file", Alice, Now);

        Assert.Equal(PlatformOwnerSupportStatus.Pending, request.Status);
        Assert.Equal("platform.licensing.approver_must_differ", Assert.Throws<DomainException>(() => request.Approve(Alice, null, Now)).Error.Code);
        Assert.Equal("platform.licensing.support_not_pending", Assert.Throws<DomainException>(() => request.Complete(Now)).Error.Code);

        request.Approve(Bob, "Checked twice", Now.AddMinutes(1));

        Assert.Equal(PlatformOwnerSupportStatus.Approved, request.Status);
        Assert.Equal(Bob, request.DecidedBy);
        Assert.Equal("platform.licensing.support_not_pending", Assert.Throws<DomainException>(() => request.Refuse(Bob, null, Now)).Error.Code);

        request.Complete(Now.AddMinutes(5));
        Assert.Equal(PlatformOwnerSupportStatus.Completed, request.Status);
        Assert.False(request.IsOpen);
    }

    [Fact]
    public void DeviceRecovery_StartsApproved_WithTheDeviceToReplace()
    {
        Guid lost = Guid.NewGuid();
        PlatformOwnerSupportRequest request = PlatformOwnerSupportRequest.StartDeviceRecovery(Tenant, "Laptop stolen", "Called back", lost, Alice, Now);

        Assert.Equal(PlatformOwnerSupportStatus.Approved, request.Status);
        Assert.Equal(lost, request.ReplaceDeviceId);
        Assert.Equal(Alice, request.DecidedBy);
    }

    [Fact]
    public void SecondFactorResetLink_LastsAnHour_AndPointsToItsRequest()
    {
        Guid support = Guid.NewGuid();
        PlatformDeviceLink link = PlatformDeviceLink.Create(PlatformDeviceLinkKind.SecondFactorReset, Tenant, null, support, new string('a', 64), Now);

        Assert.Equal(support, link.SupportRequestId);
        Assert.Equal(Now.AddMinutes(60), link.ExpiresUtc);
    }
}
