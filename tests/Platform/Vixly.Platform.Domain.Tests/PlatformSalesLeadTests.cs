using Vixly.Platform.Domain.Sales;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformSalesLeadTests.cs
//
// PURPOSE:
// Proves the sales lead from the public contact form (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - A lead keeps clean, bounded values and starts New
// - Bad input is refused with its own code
// - Status changes record who handled the lead; a repeat is no change
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformSalesLeadTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    private static readonly Guid Staff = Guid.Parse("0199e3a0-0000-7000-8000-0000000000aa");

    private static PlatformSalesLead Lead(string? phone = null, string? country = null, string? message = "We run 3 branches.\r\nToo much paper.")
    {
        return PlatformSalesLead.Create("  Rivera Freight ", "Ana Rivera", "Ana@Rivera.Example", phone, country, PlatformSalesLeadKind.Freight, message, LanguageCode.Fr, "contact", "203.0.113.7", Now);
    }

    [Fact]
    public void Create_KeepsCleanValues_AndStartsNew()
    {
        PlatformSalesLead lead = Lead(country: "us");

        Assert.Equal("Rivera Freight", lead.CompanyName);
        Assert.Equal("US", lead.CountryCode);
        Assert.Equal("We run 3 branches.\nToo much paper.", lead.Message);
        Assert.Equal(PlatformSalesLeadStatus.New, lead.Status);
        Assert.Equal(Now, lead.StatusChangedUtc);
        Assert.Equal("fr", lead.Language.Value);
        Assert.Equal("203.0.113.7", lead.ClientIp);
        Assert.Null(lead.HandledByUserId);
    }

    [Theory]
    [InlineData("", "Ana", "ana@x.example", null, null, "platform.leads.company_required")]
    [InlineData("Rivera", " ", "ana@x.example", null, null, "platform.leads.name_required")]
    [InlineData("Rivera", "Ana", "not-an-email", null, null, "platform.leads.email_invalid")]
    [InlineData("Rivera", "Ana", "ana@x.example", "call me", null, "platform.leads.phone_invalid")]
    [InlineData("Rivera", "Ana", "ana@x.example", null, "XX", "platform.leads.country_invalid")]
    [InlineData("Rivera\u0007", "Ana", "ana@x.example", null, null, "platform.leads.company_required")]
    public void Create_RefusesBadInput(string company, string name, string email, string? phone, string? country, string code)
    {
        DomainException refused = Assert.Throws<DomainException>(() =>
            PlatformSalesLead.Create(company, name, email, phone, country, PlatformSalesLeadKind.Other, null, LanguageCode.En, null, null, Now));

        Assert.Equal(code, refused.Error.Code);
    }

    [Fact]
    public void Create_RefusesAnUnknownKind_AndAnOverlongMessage()
    {
        Assert.Equal("platform.leads.kind_invalid", Assert.Throws<DomainException>(() =>
            PlatformSalesLead.Create("Rivera", "Ana", "ana@x.example", null, null, (PlatformSalesLeadKind)99, null, LanguageCode.En, null, null, Now)).Error.Code);
        Assert.Equal("platform.leads.message_too_long", Assert.Throws<DomainException>(() =>
            Lead(message: new string('m', PlatformSalesLeadLimits.MessageMaxLength + 1))).Error.Code);
    }

    [Fact]
    public void Create_ClipsTheSourceAndAddress_RatherThanRefusing()
    {
        PlatformSalesLead lead = PlatformSalesLead.Create("Rivera", "Ana", "ana@x.example", null, null, PlatformSalesLeadKind.Other, null, LanguageCode.En,
            new string('s', PlatformSalesLeadLimits.SourceMaxLength + 10), new string('1', PlatformSalesLeadLimits.IpAddressMaxLength + 10), Now);

        Assert.Equal(PlatformSalesLeadLimits.SourceMaxLength, lead.SourcePage!.Length);
        Assert.Equal(PlatformSalesLeadLimits.IpAddressMaxLength, lead.ClientIp!.Length);
    }

    [Fact]
    public void ChangeStatus_RecordsWhoHandledIt_AndARepeatIsNoChange()
    {
        PlatformSalesLead lead = Lead();
        DateTimeOffset later = Now.AddHours(2);

        Assert.True(lead.ChangeStatus(PlatformSalesLeadStatus.Contacted, " Called back ", Staff, later));
        Assert.Equal(PlatformSalesLeadStatus.Contacted, lead.Status);
        Assert.Equal(later, lead.StatusChangedUtc);
        Assert.Equal("Called back", lead.StaffNote);
        Assert.Equal(Staff, lead.HandledByUserId);

        Assert.False(lead.ChangeStatus(PlatformSalesLeadStatus.Contacted, "Called back", Staff, later.AddHours(1)));
        Assert.True(lead.ChangeStatus(PlatformSalesLeadStatus.Contacted, "Demo booked", Staff, later.AddHours(1)));
        Assert.Equal(later, lead.StatusChangedUtc);
    }

    [Fact]
    public void ChangeStatus_RefusesAnUnknownStatus_AndAnOverlongNote()
    {
        PlatformSalesLead lead = Lead();

        Assert.Equal("platform.leads.status_invalid", Assert.Throws<DomainException>(() => lead.ChangeStatus((PlatformSalesLeadStatus)0, null, Staff, Now)).Error.Code);
        Assert.Equal("platform.leads.note_too_long", Assert.Throws<DomainException>(() =>
            lead.ChangeStatus(PlatformSalesLeadStatus.Closed, new string('n', PlatformSalesLeadLimits.NoteMaxLength + 1), Staff, Now)).Error.Code);
    }
}
