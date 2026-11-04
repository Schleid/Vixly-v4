﻿using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.TenantAccount.GetTenantCompanyChange;
using Vixly.Platform.Application.UseCases.TenantAccount.SubmitTenantCompanyChange;
using Vixly.Platform.Application.UseCases.TenantAccount.UpdateTenantCompanyProfile;
using Vixly.Platform.Application.UseCases.TenantAccount.WithdrawTenantCompanyChange;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Application.UseCases.Tenants.ApprovePlatformTenantChange;
using Vixly.Platform.Application.UseCases.Tenants.GetPlatformTenantChange;
using Vixly.Platform.Application.UseCases.Tenants.GetPlatformTenantChangeProof;
using Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenantChanges;
using Vixly.Platform.Application.UseCases.Tenants.RejectPlatformTenantChange;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformTenantChangeUseCaseTests.cs
//
// PURPOSE:
// Company change requests: the tenant owner asks with proof,
// staff approve or reject (B12).
//
// RESPONSIBILITIES:
// - Prove who may submit, review and decide
// - Prove approval applies the details and records history
// - Prove one pending request per tenant and the emails
//
// IMPORTANT NOTES:
// - Proof bytes start with the PDF magic number
// =========================================================
public sealed class PlatformTenantChangeUseCaseTests
{
    private static readonly byte[] Pdf = "%PDF-1.7\n%proof"u8.ToArray();

    private readonly TenantTestKit _kit = new();

    private readonly FakeTenantChangeStore _requests;

    public PlatformTenantChangeUseCaseTests()
    {
        _requests = new FakeTenantChangeStore(_kit.Tenants);
    }

    private PlatformTenantChanges Changes() => new(
        _kit.Gate(), new PlatformStepUpGuard(_kit.Store, _kit.Clock), _kit.Store, _kit.Tenants, _requests, _kit.Audit(), _kit.Outbox(), _kit.Store, _kit.Clock);

    private static PlatformTenantAuthorizationContext As(Guid tenantId, PlatformTenantActorRole role = PlatformTenantActorRole.Owner, bool stepUp = true)
        => LicensingTestKit.Caller(tenantId, role, stepUp);

    private static PlatformCompanyInput Renamed()
    {
        PlatformCompanyInput input = TenantTestKit.Input();
        return new PlatformCompanyInput
        {
            LegalName = "Northwind Logistics Ltd.",
            TradingName = input.TradingName,
            Email = input.Email,
            Phone = input.Phone,
            AddressLine1 = input.AddressLine1,
            City = input.City,
            StateProvince = input.StateProvince,
            PostalCode = input.PostalCode,
            CountryCode = input.CountryCode
        };
    }

    private Task<SubmitTenantCompanyChangeResponse> SubmitAsync(PlatformTenant tenant, PlatformTenantAuthorizationContext? caller = null, byte[]? proof = null, PlatformCompanyInput? company = null)
    {
        return new SubmitTenantCompanyChangeUseCase(Changes()).ExecuteAsync(
            caller ?? As(tenant.Id),
            new SubmitTenantCompanyChangeRequest { Company = company ?? Renamed(), Reason = "We merged and took a new name.", ProofFileName = "certificate.pdf", ProofContent = proof ?? Pdf },
            CancellationToken.None);
    }

    [Fact]
    public async Task Submit_KeepsTheTenantAsIs_QueuesTheReviewEmail_AndAudits()
    {
        PlatformTenant tenant = _kit.Tenant(active: true);

        SubmitTenantCompanyChangeResponse response = await SubmitAsync(tenant);

        Assert.True(response.Succeeded, response.Error?.Code);
        Assert.Equal("Northwind Freight Inc.", tenant.Company.LegalName);
        PlatformTenantChangeRequest request = Assert.Single(_requests.Requests);
        Assert.True(request.IsPending);
        Assert.Equal("application/pdf", request.Proof.ContentType);
        Assert.Equal("platform.tenants.company_change_submitted", Assert.Single(_kit.Store.AuditEntries).ActionCode);
        Assert.Equal(PlatformMessageTypes.TenantCompanyChangeSubmitted, Assert.Single(_kit.Tenants.Outbox).Type);
    }

    [Fact]
    public async Task Submit_IsForTheOwner_WithAFreshSecondFactor()
    {
        PlatformTenant tenant = _kit.Tenant(active: true);

        Assert.False((await SubmitAsync(tenant, As(tenant.Id, PlatformTenantActorRole.Staff))).Succeeded);
        Assert.False((await SubmitAsync(tenant, As(tenant.Id, PlatformTenantActorRole.Owner, stepUp: false))).Succeeded);
        Assert.Empty(_requests.Requests);
    }

    [Fact]
    public async Task Submit_RefusesASecondPending_AWrongProof_AndNoChange()
    {
        PlatformTenant tenant = _kit.Tenant(active: true);

        Assert.Equal(PlatformTenantChangeErrors.ProofTypeInvalid, (await SubmitAsync(tenant, proof: "hello"u8.ToArray())).Error);
        Assert.Equal(PlatformTenantChangeErrors.NoDifference, (await SubmitAsync(tenant, company: TenantTestKit.Input())).Error);
        Assert.True((await SubmitAsync(tenant)).Succeeded);
        Assert.Equal(PlatformTenantChangeErrors.AlreadyPending, (await SubmitAsync(tenant)).Error);
    }

    [Fact]
    public async Task Approve_NeedsTheReviewPermission_AndStepUp_ThenAppliesTheDetails()
    {
        PlatformTenant tenant = _kit.Tenant(active: true);
        Guid requestId = (await SubmitAsync(tenant)).RequestId;
        ApprovePlatformTenantChangeUseCase approve = new(Changes());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => approve.ExecuteAsync(
            _kit.Staff(PlatformPermissions.TenantsView), new ApprovePlatformTenantChangeRequest { RequestId = requestId }, CancellationToken.None));

        PlatformAuthorizationContext stale = _kit.Staff(PlatformPermissions.TenantsReviewChanges);
        _kit.Clock.Advance(TimeSpan.FromMinutes(6));
        Assert.Equal(PlatformCommonErrors.StepUpRequired, (await approve.ExecuteAsync(stale, new ApprovePlatformTenantChangeRequest { RequestId = requestId }, CancellationToken.None)).Error);

        ApprovePlatformTenantChangeResponse response = await approve.ExecuteAsync(
            _kit.Staff(PlatformPermissions.TenantsReviewChanges), new ApprovePlatformTenantChangeRequest { RequestId = requestId, Note = "Certificate checked." }, CancellationToken.None);

        Assert.True(response.Succeeded, response.Error?.Code);
        Assert.Equal("Northwind Logistics Ltd.", tenant.Company.LegalName);
        Assert.Equal(PlatformTenantChangeStatus.Approved, _requests.Requests[0].Status);
        Assert.Contains(_kit.Tenants.Events, item => item.EventCode == PlatformTenantEventCodes.CompanyChanged);
        Assert.Contains(_kit.Store.AuditEntries, entry => entry.ActionCode == "platform.tenants.company_change_approved");
        Assert.Contains(_kit.Tenants.Outbox, message => message.Type == PlatformMessageTypes.TenantCompanyChangeDecided);
        Assert.Equal(PlatformTenantChangeErrors.NotPending, (await approve.ExecuteAsync(
            _kit.Staff(PlatformPermissions.TenantsReviewChanges), new ApprovePlatformTenantChangeRequest { RequestId = requestId }, CancellationToken.None)).Error);
    }

    // Owner, 2026-10-25: the company keeps its own profile; only the legal name needs approval.
    [Fact]
    public async Task Profile_TheOwnerSavesItDirectly_TheLegalNameStays()
    {
        PlatformTenant tenant = _kit.Tenant(active: true);
        PlatformCompanyInput input = TenantTestKit.Input();
        UpdateTenantCompanyProfileUseCase update = new(Changes());
        UpdateTenantCompanyProfileRequest moved = new()
        {
            Company = new PlatformCompanyInput
            {
                LegalName = "Ignored Name LLC",
                TradingName = "Northwind Express",
                Email = "hello@northwind.example",
                Phone = "+13055550100",
                AddressLine1 = "1 New Street",
                City = "Miami",
                StateProvince = input.StateProvince,
                PostalCode = "33101",
                CountryCode = "US"
            }
        };

        Assert.False((await update.ExecuteAsync(As(tenant.Id, PlatformTenantActorRole.Staff), moved, CancellationToken.None)).Succeeded);
        Assert.False((await update.ExecuteAsync(As(tenant.Id, stepUp: false), moved, CancellationToken.None)).Succeeded);

        UpdateTenantCompanyProfileResponse response = await update.ExecuteAsync(As(tenant.Id), moved, CancellationToken.None);

        Assert.True(response.Succeeded, response.Error?.Code);
        Assert.Equal("Northwind Freight Inc.", tenant.Company.LegalName);
        Assert.Equal("Northwind Express", tenant.Company.TradingName);
        Assert.Equal("Miami", tenant.Company.Address.City);
        Assert.Equal("+13055550100", tenant.Company.Phone.Value);
        Assert.Empty(_requests.Requests);
        Assert.Equal("platform.tenants.company_profile_updated", Assert.Single(_kit.Store.AuditEntries).ActionCode);
        Assert.True((await update.ExecuteAsync(As(tenant.Id), moved, CancellationToken.None)).Succeeded);
        Assert.Single(_kit.Store.AuditEntries);
    }

    // A pending legal name request must not undo profile changes made after it was sent.
    [Fact]
    public async Task Approve_ChangesTheLegalNameOnly_KeepingTheProfileAsItIsNow()
    {
        PlatformTenant tenant = _kit.Tenant(active: true);
        PlatformCompanyInput input = TenantTestKit.Input();
        Guid requestId = (await SubmitAsync(tenant)).RequestId;
        await new UpdateTenantCompanyProfileUseCase(Changes()).ExecuteAsync(As(tenant.Id), new UpdateTenantCompanyProfileRequest
        {
            Company = new PlatformCompanyInput
            {
                TradingName = input.TradingName,
                Email = input.Email,
                Phone = input.Phone,
                AddressLine1 = "99 Moved Avenue",
                City = input.City,
                StateProvince = input.StateProvince,
                PostalCode = input.PostalCode,
                CountryCode = input.CountryCode
            }
        }, CancellationToken.None);

        ApprovePlatformTenantChangeResponse response = await new ApprovePlatformTenantChangeUseCase(Changes()).ExecuteAsync(
            _kit.Staff(PlatformPermissions.TenantsReviewChanges), new ApprovePlatformTenantChangeRequest { RequestId = requestId }, CancellationToken.None);

        Assert.True(response.Succeeded, response.Error?.Code);
        Assert.Equal("Northwind Logistics Ltd.", tenant.Company.LegalName);
        Assert.Equal("99 Moved Avenue", tenant.Company.Address.Line1);
    }

    [Fact]
    public async Task Reject_NeedsANote_AndLeavesTheTenantAlone()
    {
        PlatformTenant tenant = _kit.Tenant(active: true);
        Guid requestId = (await SubmitAsync(tenant)).RequestId;
        RejectPlatformTenantChangeUseCase reject = new(Changes());
        PlatformAuthorizationContext reviewer = _kit.Staff(PlatformPermissions.TenantsReviewChanges);

        Assert.Equal(PlatformTenantChangeErrors.NoteRequired, (await reject.ExecuteAsync(reviewer, new RejectPlatformTenantChangeRequest { RequestId = requestId }, CancellationToken.None)).Error);

        RejectPlatformTenantChangeResponse response = await reject.ExecuteAsync(reviewer, new RejectPlatformTenantChangeRequest { RequestId = requestId, Note = "The certificate is not legible." }, CancellationToken.None);

        Assert.True(response.Succeeded, response.Error?.Code);
        Assert.Equal("Northwind Freight Inc.", tenant.Company.LegalName);
        Assert.Equal(PlatformTenantChangeStatus.Rejected, _requests.Requests[0].Status);
        Assert.True((await SubmitAsync(tenant)).Succeeded);
    }

    [Fact]
    public async Task Withdraw_ClosesThePendingRequest_AndTheOwnerSeesIt()
    {
        PlatformTenant tenant = _kit.Tenant(active: true);
        await SubmitAsync(tenant);

        Assert.True((await new WithdrawTenantCompanyChangeUseCase(Changes()).ExecuteAsync(As(tenant.Id), new WithdrawTenantCompanyChangeRequest(), CancellationToken.None)).Succeeded);

        GetTenantCompanyChangeResponse latest = await new GetTenantCompanyChangeUseCase(Changes()).ExecuteAsync(As(tenant.Id), new GetTenantCompanyChangeRequest(), CancellationToken.None);
        Assert.Equal(PlatformTenantChangeStatus.Withdrawn, latest.Change!.Status);
        Assert.Contains(_kit.Store.AuditEntries, entry => entry.ActionCode == "platform.tenants.company_change_withdrawn");
    }

    [Fact]
    public async Task Viewers_ListAndRead_ButOnlyReviewersMayDecide()
    {
        PlatformTenant tenant = _kit.Tenant(active: true);
        Guid requestId = (await SubmitAsync(tenant)).RequestId;
        PlatformAuthorizationContext viewer = _kit.Staff(PlatformPermissions.TenantsView);

        ListPlatformTenantChangesResponse list = await new ListPlatformTenantChangesUseCase(Changes()).ExecuteAsync(viewer, new ListPlatformTenantChangesRequest(), CancellationToken.None);
        Assert.Equal(requestId, Assert.Single(list.Changes!.Items).RequestId);

        GetPlatformTenantChangeResponse read = await new GetPlatformTenantChangeUseCase(Changes()).ExecuteAsync(viewer, new GetPlatformTenantChangeRequest { RequestId = requestId }, CancellationToken.None);
        Assert.False(read.CanDecide);
        Assert.Equal("Northwind Logistics Ltd.", read.Change!.Proposed.LegalName);

        GetPlatformTenantChangeResponse reviewer = await new GetPlatformTenantChangeUseCase(Changes()).ExecuteAsync(
            _kit.Staff(PlatformPermissions.TenantsView, PlatformPermissions.TenantsReviewChanges), new GetPlatformTenantChangeRequest { RequestId = requestId }, CancellationToken.None);
        Assert.True(reviewer.CanDecide);

        GetPlatformTenantChangeProofResponse proof = await new GetPlatformTenantChangeProofUseCase(Changes()).ExecuteAsync(viewer, new GetPlatformTenantChangeProofRequest { RequestId = requestId }, CancellationToken.None);
        Assert.Equal(Pdf, proof.File!.Content);
    }

    [Fact]
    public async Task Handlers_EmailReviewers_ThenTheOwner()
    {
        PlatformTenant tenant = _kit.Tenant(active: true);
        _kit.Staff(PlatformPermissions.TenantsReviewChanges);
        Guid requestId = (await SubmitAsync(tenant)).RequestId;

        PlatformOutboxMessage submitted = Assert.Single(_kit.Tenants.Outbox);
        await new PlatformTenantChangeSubmittedHandler(_requests, _kit.Tenants, _kit.Store, _kit.Tenants, _kit.Clock).HandleAsync(submitted, CancellationToken.None);
        Assert.Contains(_kit.Tenants.Mail, mail => mail.Kind == "change_submitted" && mail.Value == requestId.ToString("D"));

        await new RejectPlatformTenantChangeUseCase(Changes()).ExecuteAsync(
            _kit.Staff(PlatformPermissions.TenantsReviewChanges), new RejectPlatformTenantChangeRequest { RequestId = requestId, Note = "Unclear." }, CancellationToken.None);
        PlatformOutboxMessage decided = _kit.Tenants.Outbox.Last();
        await new PlatformTenantChangeDecidedHandler(_requests, _kit.Tenants, _kit.Tenants).HandleAsync(decided, CancellationToken.None);
        Assert.Contains(_kit.Tenants.Mail, mail => mail.Kind == "change_decided" && mail.To == "owner@northwind.example" && mail.Value == "Rejected");
    }
}
