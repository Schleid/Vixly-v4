using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Leads;
using Vixly.Platform.Application.UseCases.Leads.ListPlatformSalesLeads;
using Vixly.Platform.Application.UseCases.Leads.SubmitPlatformSalesLead;
using Vixly.Platform.Application.UseCases.Leads.UpdatePlatformSalesLead;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformLeadUseCaseTests.cs
//
// PURPOSE:
// Proves the contact-sales flow in Platform: the public form, the console list and the sales email (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - The form needs a passed bot check and stores the lead with the visitor address, an audit entry and one outbox message
// - The console needs leads.view to list and leads.manage to change
// - The email goes once to each holder of leads.manage and to no one else
// - Closed leads are removed after the retention
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformLeadUseCaseTests
{
    private readonly TenantTestKit _kit = new();

    private readonly FakeLeadStore _leads = new();

    private PlatformLeadAdministration Administration()
    {
        return new PlatformLeadAdministration(_kit.Gate(), _leads, _kit.Tenants, _kit.Store, _kit.Store, _kit.Audit(), _kit.Outbox(), _kit.Clock);
    }

    private static SubmitPlatformSalesLeadRequest Form(string bot = "human", string company = "Rivera Freight")
    {
        return new SubmitPlatformSalesLeadRequest
        {
            CompanyName = company,
            ContactName = "Ana Rivera",
            Email = "ana@rivera.example",
            Kind = PlatformSalesLeadKind.Freight,
            Language = "es",
            SourcePage = "contact",
            BotToken = bot
        };
    }

    private Task<SubmitPlatformSalesLeadResponse> SubmitAsync(SubmitPlatformSalesLeadRequest request)
    {
        return new SubmitPlatformSalesLeadUseCase(Administration()).ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, request, CancellationToken.None);
    }

    [Fact]
    public async Task Submit_StoresTheLead_WithTheVisitorAddress_AnAuditEntry_AndOneMessage()
    {
        SubmitPlatformSalesLeadResponse response = await SubmitAsync(Form());

        Assert.True(response.Succeeded, response.Error?.Code);
        PlatformSalesLead lead = Assert.Single(_leads.Leads);
        Assert.Equal("es", lead.Language.Value);
        Assert.Equal(_kit.Store.ClientIp, lead.ClientIp);
        Assert.Contains(_kit.Store.AuditEntries, entry => entry.ActionCode == "platform.leads.received" && entry.TargetId == lead.Id);
        Assert.Single(_kit.Tenants.Outbox, message => message.Type == PlatformMessageTypes.SalesLeadReceived);
    }

    [Theory]
    [InlineData("robot")]
    [InlineData("")]
    public async Task Submit_WithoutAPassedBotCheck_StoresNothing(string bot)
    {
        SubmitPlatformSalesLeadResponse response = await SubmitAsync(Form(bot));

        Assert.Equal(PlatformSalesLeadErrors.BotCheckFailed.Code, response.Error?.Code);
        Assert.Empty(_leads.Leads);
        Assert.Empty(_kit.Tenants.Outbox);
    }

    [Fact]
    public async Task Submit_WithBadInput_ReturnsTheDomainCode()
    {
        SubmitPlatformSalesLeadResponse response = await SubmitAsync(Form(company: " "));

        Assert.Equal(PlatformSalesLeadErrors.CompanyRequired.Code, response.Error?.Code);
        Assert.Empty(_leads.Leads);
    }

    [Fact]
    public async Task List_NeedsLeadsView_AndFiltersByStatus()
    {
        Assert.True((await SubmitAsync(Form(company: "Rivera Freight"))).Succeeded);
        Assert.True((await SubmitAsync(Form(company: "Okafor Couriers"))).Succeeded);
        _leads.Leads[0].ChangeStatus(PlatformSalesLeadStatus.Contacted, null, Guid.NewGuid(), _kit.Clock.GetUtcNow());
        ListPlatformSalesLeadsUseCase list = new(Administration());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            list.ExecuteAsync(_kit.Staff(PlatformPermissions.TenantsView), new ListPlatformSalesLeadsRequest(), CancellationToken.None));

        ListPlatformSalesLeadsResponse open = await list.ExecuteAsync(_kit.Staff(PlatformPermissions.LeadsView), new ListPlatformSalesLeadsRequest { Status = PlatformSalesLeadStatus.New }, CancellationToken.None);

        Assert.True(open.Succeeded, open.Error?.Code);
        Assert.Equal("Okafor Couriers", Assert.Single(open.Leads!.Items).CompanyName);
    }

    [Fact]
    public async Task Update_NeedsLeadsManage_AndAuditsTheChange()
    {
        Assert.True((await SubmitAsync(Form())).Succeeded);
        Guid leadId = _leads.Leads[0].Id;
        UpdatePlatformSalesLeadUseCase update = new(Administration());
        UpdatePlatformSalesLeadRequest contacted = new() { LeadId = leadId, Status = PlatformSalesLeadStatus.Contacted, Note = "Demo on Friday" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            update.ExecuteAsync(_kit.Staff(PlatformPermissions.LeadsView), contacted, CancellationToken.None));

        PlatformAuthorizationContext sales = _kit.Staff(PlatformPermissions.LeadsView, PlatformPermissions.LeadsManage);
        UpdatePlatformSalesLeadResponse response = await update.ExecuteAsync(sales, contacted, CancellationToken.None);

        Assert.True(response.Succeeded, response.Error?.Code);
        Assert.Equal(sales.PlatformUserId, _leads.Leads[0].HandledByUserId);
        Assert.Contains(_kit.Store.AuditEntries, entry => entry.ActionCode == "platform.leads.status_changed" && entry.TargetId == leadId);

        UpdatePlatformSalesLeadResponse missing = await update.ExecuteAsync(sales, new UpdatePlatformSalesLeadRequest { LeadId = Guid.NewGuid(), Status = PlatformSalesLeadStatus.Closed }, CancellationToken.None);
        Assert.Equal(PlatformSalesLeadErrors.LeadNotFound.Code, missing.Error?.Code);
    }

    [Fact]
    public async Task Mail_GoesOnceToEachHolderOfLeadsManage_AndToNoOneElse()
    {
        PlatformAuthorizationContext manager = _kit.Staff(PlatformPermissions.LeadsView, PlatformPermissions.LeadsManage);
        _kit.Staff(PlatformPermissions.LeadsView);
        Assert.True((await SubmitAsync(Form())).Succeeded);
        PlatformSalesLeadMailHandler handler = new(_leads, _kit.Store, _leads, _kit.Clock);

        foreach (PlatformOutboxMessage message in _kit.Tenants.Outbox.Where(item => item.Type == PlatformMessageTypes.SalesLeadReceived))
        {
            await handler.HandleAsync(message, CancellationToken.None);
        }

        string managerEmail = _kit.Store.Users.Single(user => user.Id == manager.PlatformUserId).Email.Value;
        (string email, Guid lead) = Assert.Single(_leads.Mails);
        Assert.Equal(managerEmail, email);
        Assert.Equal(_leads.Leads[0].Id, lead);
    }

    [Fact]
    public async Task Clock_RemovesClosedLeads_AfterTheRetention_Only()
    {
        Assert.True((await SubmitAsync(Form(company: "Old Closed"))).Succeeded);
        Assert.True((await SubmitAsync(Form(company: "Still Open"))).Succeeded);
        _leads.Leads[0].ChangeStatus(PlatformSalesLeadStatus.Closed, null, Guid.NewGuid(), _kit.Clock.GetUtcNow());
        _kit.Clock.Advance(PlatformSalesLeadLimits.ClosedRetention + TimeSpan.FromDays(1));

        int removed = await new PlatformSalesLeadClock(_leads, _kit.Clock).RunAsync(CancellationToken.None);

        Assert.Equal(1, removed);
        Assert.Equal("Still Open", Assert.Single(_leads.Leads).CompanyName);
    }
}
