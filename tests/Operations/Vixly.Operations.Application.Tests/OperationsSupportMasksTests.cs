using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Support;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsSupportMasksTests.cs
//
// PURPOSE:
// Proves what a support agent sees of a customer, and that an outside account holds only the support lookup (owner, 2026-10-29, design section 9).
//
// RESPONSIBILITIES:
// - Masked phone, email and name; one phone written two ways
// - An outside account's roles give nothing but the support lookup
//
// IMPORTANT NOTES:
// - Pure functions and the access view; no database
// =========================================================
public sealed class OperationsSupportMasksTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Tenant = Guid.NewGuid();

    [Fact]
    public void Masks_ShowAlmostNothing()
    {
        Assert.Equal("\u2022\u2022\u2022-\u2022\u2022\u2022-4521", OperationsSupportMasks.Phone("+1 (305) 555-4521"));
        Assert.Equal("j\u2022\u2022\u2022@g\u2022\u2022\u2022.com", OperationsSupportMasks.Email("jean.pierre@gmail.com"));
        Assert.Equal("Jean P.", OperationsSupportMasks.ShortName("Jean", "pierre"));
        Assert.Null(OperationsSupportMasks.Email("not-an-email"));
        Assert.True(OperationsSupportMasks.SamePhone("+509 4860-5107", "48605107"));
        Assert.False(OperationsSupportMasks.SamePhone("+509 4860-5107", "5107"));
        Assert.False(OperationsSupportMasks.SamePhone("+509 4860-5107", "+509 4860-5108"));
    }

    [Fact]
    public void OutsideAccount_HoldsOnlyTheSupportLookup_WhateverItsRoles()
    {
        OperationsUser agent = OperationsUser.InviteStaff(Tenant, "Call Agent", EmailAddress.Create("agent@callcentre.example"), LanguageCode.En, Now);
        OperationsRole wide = OperationsRole.Create(Tenant, "Too much", new[] { OperationsPermissions.SupportLookup, OperationsPermissions.CustomersView, OperationsPermissions.ShipmentsView }, Now);
        (OperationsRoleGrant, OperationsRole)[] grants = { (OperationsRoleGrant.Grant(Tenant, agent.Id, wide.Id, OperationsRoleScope.Tenant, null, null, Now), wide) };

        Assert.True(new OperationsAccessView(agent, grants).Has(OperationsPermissions.CustomersView));

        agent.MakeOutside(Vixly.Operations.Domain.Identity.OperationsOutsideHours.Create(null, 127, null, null), new DateOnly(2026, 10, 29), Now);
        OperationsAccessView view = new(agent, grants);

        Assert.True(view.HasTenantWide(OperationsPermissions.SupportLookup));
        Assert.False(view.Has(OperationsPermissions.CustomersView));
        Assert.False(view.HasTenantWide(OperationsPermissions.ShipmentsView));
    }
}
