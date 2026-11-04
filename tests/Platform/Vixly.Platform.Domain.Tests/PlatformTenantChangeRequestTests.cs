﻿using Vixly.Platform.Domain.Tenants;
using static Vixly.Platform.Domain.Tests.TenantTestData;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformTenantChangeRequestTests.cs
//
// PURPOSE:
// Rules of a company change request (B12).
//
// RESPONSIBILITIES:
// - Prove the reason, difference and proof checks
// - Prove approve, reject and withdraw move only a pending request
//
// IMPORTANT NOTES:
// - Proof type comes from the file's first bytes, not its name
// =========================================================
public sealed class PlatformTenantChangeRequestTests
{
    private static readonly byte[] Pdf = "%PDF-1.7 proof"u8.ToArray();

    private static readonly byte[] Jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 1, 2 };

    private static PlatformTenantChangeRequest Submit(PlatformTenant tenant, string? reason = "We merged.", byte[]? proof = null, string legalName = "Northwind Logistics Ltd.")
    {
        return PlatformTenantChangeRequest.Submit(tenant, Company(legalName), reason, "proof.pdf", proof ?? Pdf, "ops-1", "Grace Hopper", Now);
    }

    [Fact]
    public void Submit_KeepsBothVersions_AndDetectsTheProofType()
    {
        PlatformTenant tenant = InStatus(PlatformTenantStatus.Active);

        PlatformTenantChangeRequest request = Submit(tenant, proof: Jpeg);

        Assert.True(request.IsPending);
        Assert.Equal(("Northwind Freight Inc.", "Northwind Logistics Ltd."), (request.Previous.LegalName, request.Proposed.LegalName));
        Assert.Equal(("image/jpeg", Jpeg.Length, 64), (request.Proof.ContentType, request.Proof.SizeBytes, request.Proof.Sha256.Length));
    }

    [Fact]
    public void Submit_RefusesNoReason_NoChange_AndBadProof()
    {
        PlatformTenant tenant = InStatus(PlatformTenantStatus.Active);

        AssertError(PlatformTenantChangeErrors.ReasonRequired, () => Submit(tenant, reason: " "));
        AssertError(PlatformTenantChangeErrors.ReasonTooLong, () => Submit(tenant, reason: new string('x', PlatformTenantChangeLimits.ReasonMaxLength + 1)));
        AssertError(PlatformTenantChangeErrors.NoDifference, () => Submit(tenant, legalName: "Northwind Freight Inc."));
        AssertError(PlatformTenantChangeErrors.ProofRequired, () => Submit(tenant, proof: Array.Empty<byte>()));
        AssertError(PlatformTenantChangeErrors.ProofTypeInvalid, () => Submit(tenant, proof: "<html>"u8.ToArray()));
        AssertError(PlatformTenantChangeErrors.ProofTooLarge, () => Submit(tenant, proof: new byte[PlatformTenantChangeLimits.ProofMaxBytes + 1]));
    }

    [Fact]
    public void Approve_AppliesTheDetails_AndRecordsHistory_Once()
    {
        PlatformTenant tenant = InStatus(PlatformTenantStatus.Active);
        PlatformTenantChangeRequest request = Submit(tenant);

        PlatformTenantLifecycleEvent history = request.Approve(tenant, null, Actor, Now.AddHours(1));

        Assert.Equal(PlatformTenantEventCodes.CompanyChanged, history.EventCode);
        Assert.Equal("Northwind Logistics Ltd.", tenant.Company.LegalName);
        Assert.Equal((PlatformTenantChangeStatus.Approved, Actor), (request.Status, request.DecidedByUserId!.Value));
        AssertError(PlatformTenantChangeErrors.NotPending, () => request.Approve(tenant, null, Actor, Now.AddHours(2)));
        AssertError(PlatformTenantChangeErrors.NotPending, () => request.Withdraw(Now.AddHours(2)));
    }

    [Fact]
    public void Reject_NeedsANote_AndWithdrawCloses()
    {
        PlatformTenant tenant = InStatus(PlatformTenantStatus.Active);
        PlatformTenantChangeRequest rejected = Submit(tenant);

        AssertError(PlatformTenantChangeErrors.NoteRequired, () => rejected.Reject(" ", Actor, Now));
        rejected.Reject("Proof unreadable.", Actor, Now);
        Assert.Equal(PlatformTenantChangeStatus.Rejected, rejected.Status);
        Assert.Equal("Northwind Freight Inc.", tenant.Company.LegalName);

        PlatformTenantChangeRequest withdrawn = Submit(tenant);
        withdrawn.Withdraw(Now);
        Assert.Equal(PlatformTenantChangeStatus.Withdrawn, withdrawn.Status);
        AssertError(PlatformTenantChangeErrors.NotPending, () => withdrawn.Reject("Late.", Actor, Now));
    }
}
