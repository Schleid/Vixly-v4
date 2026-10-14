using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;
using static Vixly.Platform.Domain.Tests.TenantTestData;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformTenantSlugAndCodeTests.cs
//
// PURPOSE:
// Proves portal slug and tenant code rules (D-T3, D-T10, D-T11,
// D-T13).
//
// RESPONSIBILITIES:
// - Slugs are DNS labels; reserved names are refused
// - A slug can be corrected only while the tenant has never been
//   active
// - Slug claims hold, lapse and settle correctly
// - Tenant codes are generated and validated
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformTenantSlugAndCodeTests
{
    [Theory]
    [InlineData("northwind", "northwind")]
    [InlineData(" North-Wind ", "north-wind")]
    [InlineData("a1b", "a1b")]
    public void ValidSlug_IsNormalised(string input, string expected)
    {
        Assert.Equal(expected, PlatformPortalSlugFormat.Normalize(input));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("-north")]
    [InlineData("north-")]
    [InlineData("north--wind")]
    [InlineData("north_wind")]
    [InlineData("north.wind")]
    [InlineData("nörth")]
    [InlineData("")]
    public void InvalidSlug_IsRefused(string input)
    {
        AssertError(PlatformTenantErrors.SlugInvalid, () => PlatformPortalSlugFormat.Normalize(input));
    }

    [Fact]
    public void SlugOfMaximumLength_IsAccepted_AndOneMoreIsRefused()
    {
        Assert.Equal(63, PlatformPortalSlugFormat.Normalize(new string('a', 63)).Length);
        AssertError(PlatformTenantErrors.SlugInvalid, () => PlatformPortalSlugFormat.Normalize(new string('a', 64)));
    }

    [Theory]
    [InlineData("www")]
    [InlineData("vixly-api")]
    [InlineData("acme-staging")]
    [InlineData("staging")]
    public void ReservedFloor_CannotBeRemovedByConfiguration(string slug)
    {
        Assert.True(PlatformPortalSlugFormat.IsReserved(slug, Array.Empty<string>()));
    }

    [Fact]
    public void ConfiguredNames_AreAddedToTheFloor()
    {
        Assert.True(PlatformPortalSlugFormat.IsReserved("billing", new[] { " Billing " }));
        Assert.False(PlatformPortalSlugFormat.IsReserved("northwind", new[] { "billing" }));
    }

    [Fact]
    public void Slug_CanBeCorrected_WhileNeverActive()
    {
        PlatformTenant tenant = Draft();

        PlatformTenantLifecycleEvent corrected = tenant.CorrectSlug("North-Wind", "Typo at sign-up", Actor, Now);

        Assert.Equal("north-wind", tenant.CurrentSlug);
        Assert.Equal(PlatformTenantEventCodes.SlugCorrected, corrected.EventCode);
        Assert.Equal("northwind", corrected.Arguments["from"]);
        Assert.Equal("north-wind", corrected.Arguments["to"]);
        AssertError(PlatformTenantErrors.SlugUnchanged, () => tenant.CorrectSlug("north-wind", "again", Actor, Now));
    }

    [Theory]
    [InlineData(PlatformTenantStatus.Active)]
    [InlineData(PlatformTenantStatus.Suspended)]
    [InlineData(PlatformTenantStatus.Deactivated)]
    [InlineData(PlatformTenantStatus.Banned)]
    public void Slug_IsFixed_OnceTheTenantHasBeenActiveOrIsClosed(PlatformTenantStatus status)
    {
        PlatformTenant tenant = InStatus(status);

        AssertError(PlatformTenantErrors.SlugFixed, () => tenant.CorrectSlug("other-name", "Rebrand", Actor, Now));
        Assert.Equal("northwind", tenant.CurrentSlug);
    }

    [Fact]
    public void Slug_StaysFixed_AfterAnActiveTenantIsReactivatedOrDeactivated()
    {
        PlatformTenant tenant = InStatus(PlatformTenantStatus.Deactivated);
        tenant.Reactivate("Back", Actor, Now);

        Assert.Equal(PlatformTenantStatus.Active, tenant.Status);
        Assert.False(tenant.CanCorrectSlug);
    }

    [Fact]
    public void PendingClaim_Lapses_AndCanThenBeTaken()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        PlatformPortalSlug claim = PlatformPortalSlug.ClaimForRegistration("northwind", first, Now.AddHours(24), Now);

        AssertError(PlatformTenantErrors.SlugTaken, () => claim.ReclaimForRegistration(second, Now.AddHours(30), Now.AddHours(23)));

        claim.ReclaimForRegistration(second, Now.AddHours(48), Now.AddHours(24));

        Assert.True(claim.IsHeldBy(second));
        Assert.False(claim.IsHeldBy(first));
    }

    [Fact]
    public void PermanentClaim_NeverLapses()
    {
        Guid registration = Guid.NewGuid();
        Guid tenant = Guid.NewGuid();
        PlatformPortalSlug claim = PlatformPortalSlug.ClaimForRegistration("northwind", registration, Now.AddHours(24), Now);

        claim.MakePermanent(registration, tenant, Now.AddHours(1));

        Assert.Equal(PlatformPortalSlugState.Permanent, claim.State);
        Assert.Equal(tenant, claim.PlatformTenantId);
        Assert.False(claim.IsLapsed(Now.AddYears(10)));
        AssertError(PlatformTenantErrors.SlugTaken, () => claim.ReclaimForTenant(Guid.NewGuid(), Now.AddYears(10)));
    }

    [Fact]
    public void OnlyTheHolder_CanSettleOrExtendAPendingClaim()
    {
        PlatformPortalSlug claim = PlatformPortalSlug.ClaimForRegistration("northwind", Guid.NewGuid(), Now.AddHours(24), Now);

        AssertError(PlatformTenantErrors.SlugTaken, () => claim.MakePermanent(Guid.NewGuid(), Guid.NewGuid(), Now));
        AssertError(PlatformTenantErrors.SlugTaken, () => claim.ExtendHold(Guid.NewGuid(), Now.AddHours(48), Now));
    }

    [Fact]
    public void NewCode_IsTenPlusSixCrockfordCharacters()
    {
        string code = PlatformTenantCodeFormat.FromRandom(new byte[] { 0xFF, 0x00, 0xA5, 0x3C });

        Assert.Matches("^TEN-[0-9ABCDEFGHJKMNPQRSTVWXYZ]{6}$", code);
        Assert.Equal(code, PlatformTenantCodeFormat.Validate(code));
        Assert.NotEqual(code, PlatformTenantCodeFormat.FromRandom(new byte[] { 0xFF, 0x00, 0xA5, 0x3D }));
    }

    [Theory]
    [InlineData("TEN-1A2B")]
    [InlineData("ten-00ff")]
    public void V3Codes_StayValid(string code)
    {
        Assert.Equal(code.ToUpperInvariant(), PlatformTenantCodeFormat.Validate(code));
    }

    [Theory]
    [InlineData("TE")]
    [InlineData("TEN-")]
    [InlineData("TEN 1")]
    public void BadCode_IsRefused(string code)
    {
        AssertError(PlatformTenantErrors.CodeInvalid, () => PlatformTenantCodeFormat.Validate(code));
    }
}
