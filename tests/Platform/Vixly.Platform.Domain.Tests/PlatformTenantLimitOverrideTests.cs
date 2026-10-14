using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformTenantLimitOverrideTests.cs
//
// PURPOSE:
// Proves limit overrides (D-S8).
//
// RESPONSIBILITIES:
// - Overrides need a reason, a valid value and a future end; they apply until removed or ended
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformTenantLimitOverrideTests
{
    private static readonly DateTimeOffset Now = SubscriptionTestData.Now;

    [Fact]
    public void Override_AppliesUntilItsEnd_OrUntilRemoved()
    {
        PlatformTenantLimitOverride limit = PlatformTenantLimitOverride.Set(
            SubscriptionTestData.Tenant, PlatformPlanLimitKind.MaxWarehouses, 5, "Seasonal peak", Now.AddDays(30), SubscriptionTestData.Actor, Now);

        Assert.True(limit.AppliesAt(Now.AddDays(29)));
        Assert.False(limit.AppliesAt(Now.AddDays(30)));

        limit.Remove(SubscriptionTestData.Actor, Now.AddDays(1));
        Assert.False(limit.AppliesAt(Now.AddDays(2)));
        SubscriptionTestData.AssertCode("platform.subscriptions.override_closed", () => limit.Remove(SubscriptionTestData.Actor, Now.AddDays(1)));
    }

    [Fact]
    public void Override_RefusesBadInput()
    {
        SubscriptionTestData.AssertCode("platform.subscriptions.reason_required", () => PlatformTenantLimitOverride.Set(SubscriptionTestData.Tenant, PlatformPlanLimitKind.MaxWarehouses, 5, " ", null, SubscriptionTestData.Actor, Now));
        SubscriptionTestData.AssertCode("platform.subscriptions.override_value_invalid", () => PlatformTenantLimitOverride.Set(SubscriptionTestData.Tenant, PlatformPlanLimitKind.MaxWarehouses, -1, "Why", null, SubscriptionTestData.Actor, Now));
        SubscriptionTestData.AssertCode("platform.subscriptions.override_end_invalid", () => PlatformTenantLimitOverride.Set(SubscriptionTestData.Tenant, PlatformPlanLimitKind.MaxWarehouses, 5, "Why", Now, SubscriptionTestData.Actor, Now));
        SubscriptionTestData.AssertCode("platform.subscriptions.override_limit_invalid", () => PlatformTenantLimitOverride.Set(SubscriptionTestData.Tenant, (PlatformPlanLimitKind)99, 5, "Why", null, SubscriptionTestData.Actor, Now));
    }
}
