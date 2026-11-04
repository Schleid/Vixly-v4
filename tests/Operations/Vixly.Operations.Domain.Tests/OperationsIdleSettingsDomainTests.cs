using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsIdleSettingsDomainTests.cs
//
// PURPOSE:
// Proves the company's inactivity sign-out settings (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Defaults are the old fixed times: staff 15, customers 60, the warning 2
// - The ranges and the warning-before-sign-out rule
// - Sessions end at the time given, not the old fixed one
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsIdleSettingsDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly string Hash = new('a', 64);

    [Fact]
    public void Defaults_AreTheTimesThatWereFixed()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);

        Assert.Equal(15, settings.StaffIdleMinutes);
        Assert.Equal(60, settings.CustomerIdleMinutes);
        Assert.Equal(2, settings.IdleWarnMinutes);
    }

    [Theory]
    [InlineData(4, 60, 2, "ops.settings.staff_idle_invalid")]
    [InlineData(61, 60, 2, "ops.settings.staff_idle_invalid")]
    [InlineData(15, 14, 2, "ops.settings.customer_idle_invalid")]
    [InlineData(15, 241, 2, "ops.settings.customer_idle_invalid")]
    [InlineData(15, 60, 0, "ops.settings.idle_warn_invalid")]
    [InlineData(15, 60, 6, "ops.settings.idle_warn_invalid")]
    [InlineData(5, 60, 5, "ops.settings.idle_warn_invalid")]
    public void Times_OutsideTheirRange_AreRefused(int staff, int customer, int warn, string code)
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);

        Assert.Equal(code, Assert.Throws<DomainException>(() => settings.ChangeIdle(staff, customer, warn, Now)).Error.Code);
        Assert.Equal(15, settings.StaffIdleMinutes);
    }

    [Fact]
    public void Times_InRange_AreKept()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);

        settings.ChangeIdle(30, 240, 5, Now);

        Assert.Equal((30, 240, 5), (settings.StaffIdleMinutes, settings.CustomerIdleMinutes, settings.IdleWarnMinutes));
    }

    [Fact]
    public void Sessions_EndAtTheTimeGiven()
    {
        OperationsUserSession staff = OperationsUserSession.Start(Tenant, Guid.NewGuid(), Hash, null, null, false, Now);
        OperationsCustomerSession customer = OperationsCustomerSession.Start(Tenant, Guid.NewGuid(), Hash, null, null, Now);

        Assert.Null(staff.ExpiryReasonAt(Now.AddMinutes(25), TimeSpan.FromMinutes(30)));
        Assert.Equal(OperationsSessionEndReason.IdleTimeout, staff.ExpiryReasonAt(Now.AddMinutes(30), TimeSpan.FromMinutes(30)));
        Assert.Equal(OperationsSessionEndReason.IdleTimeout, staff.ExpiryReasonAt(Now.AddMinutes(6), TimeSpan.FromMinutes(5)));
        Assert.Null(customer.ExpiryReasonAt(Now.AddMinutes(100), TimeSpan.FromMinutes(120)));
        Assert.Equal(OperationsSessionEndReason.IdleTimeout, customer.ExpiryReasonAt(Now.AddMinutes(120), TimeSpan.FromMinutes(120)));
    }
}
