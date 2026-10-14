using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;
using static Vixly.Platform.Domain.Tests.TenantTestData;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformTenantProfileTests.cs
//
// PURPOSE:
// Proves company details, contacts, tags, settings and notes
// (D-T15, D-T16).
//
// RESPONSIBILITIES:
// - Company and address fields are validated
// - Contacts need one owner and unique roles; billing falls back
//   to owner
// - Tags are normalised, unique and limited
// - Time zones are IANA ids; countries are ISO codes
// - Notes are required and bounded
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformTenantProfileTests
{
    [Fact]
    public void Company_IsTrimmed_AndDisplayNamePrefersTradingName()
    {
        PlatformCompanyDetails company = Company("  Northwind Freight Inc.  ");

        Assert.Equal("Northwind Freight Inc.", company.LegalName);
        Assert.Equal("Northwind", company.DisplayName);
        Assert.Equal("CA", company.Address.CountryCode);
    }

    [Fact]
    public void Company_NeedsALegalNameWithinItsLimit()
    {
        AssertError(PlatformTenantErrors.LegalNameRequired, () => Company(" "));
        AssertError(PlatformTenantErrors.LegalNameTooLong, () => Company(new string('n', 201)));
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("CAN")]
    [InlineData("")]
    public void Address_NeedsAKnownCountry(string country)
    {
        AssertError(
            PlatformTenantErrors.CountryInvalid,
            () => PlatformPostalAddress.Create("1 Road", null, "City", null, null, country));
    }

    [Fact]
    public void Address_NeedsALineAndACity()
    {
        AssertError(PlatformTenantErrors.AddressRequired, () => PlatformPostalAddress.Create(" ", null, "City", null, null, "CA"));
        AssertError(PlatformTenantErrors.CityRequired, () => PlatformPostalAddress.Create("1 Road", null, null, null, null, "CA"));
    }

    [Fact]
    public void CountryList_HasEveryAssignedCode()
    {
        Assert.Equal(249, PlatformCountryCodes.All.Count);
    }

    [Theory]
    [InlineData("America/Toronto", "America/Toronto")]
    [InlineData("Europe/Paris", "Europe/Paris")]
    [InlineData("", "UTC")]
    [InlineData("utc", "UTC")]
    public void TimeZone_AcceptsIanaIds(string input, string expected)
    {
        Assert.Equal(expected, PlatformTimeZones.Normalize(input));
    }

    [Theory]
    [InlineData("Eastern Standard Time")]
    [InlineData("Mars/Olympus")]
    public void TimeZone_RefusesWindowsAndUnknownIds(string input)
    {
        AssertError(PlatformTenantErrors.TimeZoneInvalid, () => PlatformTimeZones.Normalize(input));
    }

    [Fact]
    public void Contacts_NeedOneOwner_AndUniqueRoles()
    {
        PlatformTenant tenant = Draft();

        AssertError(
            PlatformTenantErrors.OwnerContactRequired,
            () => tenant.ReplaceContacts(new[] { Contact(PlatformTenantContactRole.Billing) }, Now));
        AssertError(
            PlatformTenantErrors.ContactRoleDuplicate,
            () => tenant.ReplaceContacts(new[] { Contact(), Contact() }, Now));
    }

    [Fact]
    public void Contacts_AreReplacedByRole_AndBillingFallsBackToOwner()
    {
        PlatformTenant tenant = Draft();
        Guid ownerId = tenant.Contacts.Single().Id;

        Assert.Equal("owner@northwind.example", tenant.ContactFor(PlatformTenantContactRole.Billing).Email.Value);

        bool changed = tenant.ReplaceContacts(
            new[] { Contact(), Contact(PlatformTenantContactRole.Billing, "billing@northwind.example") },
            Now);

        Assert.True(changed);
        Assert.Equal(ownerId, tenant.ContactFor(PlatformTenantContactRole.Owner).Id);
        Assert.Equal("billing@northwind.example", tenant.ContactFor(PlatformTenantContactRole.Billing).Email.Value);

        Assert.True(tenant.ReplaceContacts(new[] { Contact() }, Now));
        Assert.Single(tenant.Contacts);
        Assert.False(tenant.ReplaceContacts(new[] { Contact() }, Now));
    }

    [Fact]
    public void Tags_AreNormalisedAndUnique()
    {
        PlatformTenant tenant = Draft();

        tenant.SetTags(new[] { "Enterprise", " enterprise ", "pilot-2026" }, Now);

        Assert.Equal(new[] { "enterprise", "pilot-2026" }, tenant.Tags.Select(tag => tag.Value).Order(StringComparer.Ordinal));
        Assert.True(tenant.SetTags(new[] { "pilot-2026" }, Now));
        Assert.Equal("pilot-2026", Assert.Single(tenant.Tags).Value);
        Assert.False(tenant.SetTags(new[] { "pilot-2026" }, Now));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("has space")]
    [InlineData("-lead")]
    [InlineData("été")]
    public void BadTag_IsRefused(string tag)
    {
        AssertError(PlatformTenantErrors.TagInvalid, () => Draft().SetTags(new[] { tag }, Now));
    }

    [Fact]
    public void Tags_AreLimited()
    {
        string[] eleven = Enumerable.Range(1, 11).Select(index => $"tag-{index}").ToArray();

        AssertError(PlatformTenantErrors.TooManyTags, () => Draft().SetTags(eleven, Now));
    }

    [Fact]
    public void Settings_AndAccountManager_AreValidated()
    {
        PlatformTenant tenant = Draft();
        Guid manager = Guid.NewGuid();

        tenant.UpdateSettings(LanguageCode.Es, "Europe/Madrid", Now);
        tenant.AssignAccountManager(manager, Now);

        Assert.Equal(LanguageCode.Es, tenant.DefaultLanguage);
        Assert.Equal("Europe/Madrid", tenant.TimeZone);
        Assert.Equal(manager, tenant.AccountManagerUserId);
        AssertError(PlatformTenantErrors.AccountManagerInvalid, () => tenant.AssignAccountManager(Guid.Empty, Now));

        tenant.AssignAccountManager(null, Now);
        Assert.Null(tenant.AccountManagerUserId);
    }

    [Fact]
    public void StaffCreation_StartsInDraft_WithItsHistoryEvent()
    {
        PlatformTenantCreation creation = PlatformTenant.CreateByStaff(
            "TEN-7K3M9Q",
            "northwind",
            Company(),
            Contact(),
            LanguageCode.Fr,
            null,
            Actor,
            Now);

        Assert.Equal(PlatformTenantStatus.Draft, creation.Tenant.Status);
        Assert.Equal(PlatformTenantOrigin.Staff, creation.Tenant.Origin);
        Assert.Equal(PlatformTimeZones.Utc, creation.Tenant.TimeZone);
        PlatformTenantLifecycleEvent created = Assert.Single(creation.Events);
        Assert.Equal(PlatformTenantEventCodes.CreatedByStaff, created.EventCode);
        Assert.Null(created.FromStatus);
    }

    [Fact]
    public void Note_IsRequired_AndBounded()
    {
        PlatformTenantNote note = PlatformTenantNote.Write(Guid.NewGuid(), "  Called about renewal  ", Actor, Now);

        Assert.Equal("Called about renewal", note.Body);
        AssertError(PlatformTenantErrors.NoteRequired, () => PlatformTenantNote.Write(Guid.NewGuid(), " ", Actor, Now));
        AssertError(PlatformTenantErrors.NoteTooLong, () => PlatformTenantNote.Write(Guid.NewGuid(), new string('x', 4001), Actor, Now));
        AssertError(PlatformTenantErrors.TenantRequired, () => PlatformTenantNote.Write(Guid.Empty, "x", Actor, Now));
    }
}
