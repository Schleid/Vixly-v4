using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsInsuranceDomainTests.cs
//
// PURPOSE:
// Proves the rules of the insurance module: policies, liability terms, certificates (Slice 5B step 1).
//
// RESPONSIBILITIES:
// - A policy's kind fits what it covers; insurer and number unless self-insured; dates, amounts, documents within limits
// - What the company owes on a shipment the customer did not insure, per basis
// - A certificate is provisional, renewed, voided, made final once
//
// IMPORTANT NOTES:
// - Pure domain, no database
// =========================================================
public sealed class OperationsInsuranceDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    private static OperationsPolicyTerms Terms(OperationsPolicyScope scope = OperationsPolicyScope.CustomerShipments, OperationsPolicyKind kind = OperationsPolicyKind.OpenCargo,
        string? insurer = "Atlantic Marine", string? number = "OC-1", DateOnly? starts = null, DateOnly? ends = null, decimal? deductible = 250m, string currency = "usd") =>
        new(scope, kind, insurer, null, null, null, null, null, null, null, number, starts ?? new DateOnly(2026, 1, 1), ends ?? new DateOnly(2026, 12, 31), currency,
            25000m, null, null, null, deductible, null, null, OperationsPolicyRisks.Loss | OperationsPolicyRisks.Damage, OperationsPolicyModes.Air, null,
            OperationsInsuranceExclusions.Standard, null, 30, OperationsDeclarationFrequency.Monthly, null);

    [Fact]
    public void Policy_KindFitsWhatItCovers_AndNeedsItsInsurer_UnlessSelfInsured()
    {
        OperationsInsurancePolicy policy = OperationsInsurancePolicy.Create(Tenant, Terms(), Now);
        Assert.Equal(("USD", OperationsDeclarationFrequency.Monthly, "Atlantic Marine OC-1"), (policy.CurrencyCode, policy.Declarations, policy.Name));

        Assert.Equal("ops.insurance.kind_invalid", Assert.Throws<DomainException>(() =>
            OperationsInsurancePolicy.Create(Tenant, Terms(OperationsPolicyScope.CompanyOperations, OperationsPolicyKind.OpenCargo), Now)).Error.Code);
        Assert.Equal("ops.insurance.kind_invalid", Assert.Throws<DomainException>(() =>
            OperationsInsurancePolicy.Create(Tenant, Terms(OperationsPolicyScope.CustomerShipments, OperationsPolicyKind.WarehouseLegalLiability), Now)).Error.Code);
        Assert.Equal("ops.insurance.details_invalid", Assert.Throws<DomainException>(() => OperationsInsurancePolicy.Create(Tenant, Terms(insurer: " "), Now)).Error.Code);
        Assert.Equal("ops.insurance.dates_invalid", Assert.Throws<DomainException>(() =>
            OperationsInsurancePolicy.Create(Tenant, Terms(starts: new DateOnly(2026, 6, 1), ends: new DateOnly(2026, 5, 1)), Now)).Error.Code);
        Assert.Equal("ops.insurance.amount_invalid", Assert.Throws<DomainException>(() => OperationsInsurancePolicy.Create(Tenant, Terms(deductible: -1m), Now)).Error.Code);
        Assert.Equal("ops.insurance.details_invalid", Assert.Throws<DomainException>(() => OperationsInsurancePolicy.Create(Tenant, Terms(currency: "US"), Now)).Error.Code);

        // Self-insured: no insurer or number; the operations scope never declares shipments.
        OperationsInsurancePolicy own = OperationsInsurancePolicy.Create(Tenant, Terms(OperationsPolicyScope.CompanyOperations, OperationsPolicyKind.SelfInsured, null, null), Now);
        Assert.Equal(OperationsDeclarationFrequency.None, own.Declarations);
    }

    [Fact]
    public void Policy_InForceAndEndingSoon_ArchivedIsNeither_AndKeepsItsDocuments()
    {
        OperationsInsurancePolicy policy = OperationsInsurancePolicy.Create(Tenant, Terms(), Now);

        Assert.True(policy.IsInForce(new DateOnly(2026, 6, 1)));
        Assert.False(policy.EndsSoon(new DateOnly(2026, 6, 1)));
        Assert.True(policy.EndsSoon(new DateOnly(2026, 12, 10)));
        Assert.False(policy.IsInForce(new DateOnly(2027, 1, 2)));
        Assert.True(policy.Covers(OperationsTransportMode.Air));
        Assert.False(policy.Covers(OperationsTransportMode.Ocean));

        OperationsPolicyDocument document = policy.AttachDocument("policy", "C:\\scans\\policy \"2026\".pdf", "key-1", "application/pdf", new string('a', 64), Now);
        Assert.Equal("policy 2026.pdf", document.FileName);
        Assert.Equal("ops.insurance.document_invalid", Assert.Throws<DomainException>(() => policy.AttachDocument("invoice", "a.pdf", "key-2", "application/pdf", "x", Now)).Error.Code);
        Assert.Equal(document.Id, policy.RemoveDocument(document.Id, Now).Id);
        Assert.Empty(policy.Documents);

        for (int index = 0; index < OperationsInsurancePolicy.MaxDocuments; index++)
        {
            policy.AttachDocument("endorsement", "e.pdf", "key-" + index, "application/pdf", "x", Now);
        }

        Assert.Equal("ops.insurance.too_many_documents", Assert.Throws<DomainException>(() => policy.AttachDocument("other", "o.pdf", "key-x", "application/pdf", "x", Now)).Error.Code);
        policy.Archive(true, Now);
        Assert.False(policy.IsInForce(new DateOnly(2026, 6, 1)));
        Assert.False(policy.EndsSoon(new DateOnly(2026, 12, 10)));
    }

    [Fact]
    public void Liability_OwesPerBasis_UpToTheCap()
    {
        OperationsLiabilityTerms terms = OperationsLiabilityTerms.Create(Tenant, OperationsTransportMode.Air, Now);
        Assert.Equal(0m, terms.Limit(500m, 10m));

        // $9.07 per lb, at most $100 a parcel: a 4.5359237 kg parcel is 10 lb.
        terms.Change(OperationsLiabilityBasis.PerWeight, 9.07m, OperationsWeightUnit.Pound, 100m, null, Now);
        Assert.Equal(90.70m, terms.Limit(500m, 4.5359237m));
        Assert.Equal(100m, terms.Limit(500m, 20m));

        terms.Change(OperationsLiabilityBasis.PerParcel, 50m, OperationsWeightUnit.Kilogram, null, "Up to $50 a parcel.", Now);
        Assert.Equal(50m, terms.Limit(500m, 3m));

        terms.Change(OperationsLiabilityBasis.DeclaredValueUpToCap, null, OperationsWeightUnit.Kilogram, 200m, null, Now);
        Assert.Equal((120m, 200m), (terms.Limit(120m, null), terms.Limit(800m, null)));
        Assert.Null(terms.Amount);

        Assert.Equal("ops.insurance.liability_invalid", Assert.Throws<DomainException>(() =>
            terms.Change(OperationsLiabilityBasis.PerWeight, null, OperationsWeightUnit.Kilogram, null, null, Now)).Error.Code);
        Assert.Equal("ops.insurance.liability_invalid", Assert.Throws<DomainException>(() =>
            terms.Change(OperationsLiabilityBasis.DeclaredValueUpToCap, null, OperationsWeightUnit.Kilogram, null, null, Now)).Error.Code);
    }

    [Fact]
    public void Certificate_IsProvisional_RenewedOrVoided_AndFinalOnce()
    {
        Guid plan = Guid.NewGuid();
        Guid policy = Guid.NewGuid();
        OperationsInsuranceCertificate certificate = OperationsInsuranceCertificate.Issue(Tenant, "IC26000001", Guid.NewGuid(), Guid.NewGuid(), plan, policy, OperationsTransportMode.Air,
            Now, Now);
        Assert.Equal((OperationsCertificateStatus.Provisional, true), (certificate.Status, certificate.IsLive));

        Guid other = Guid.NewGuid();
        certificate.Renew(other, null, OperationsTransportMode.Ocean, Now.AddHours(1), Now);
        Assert.Equal((other, (Guid?)null, OperationsTransportMode.Ocean), (certificate.PlanId, certificate.PolicyId, certificate.Mode));

        certificate.MakeFinal(640m, 19.20m, "USD", Now);
        certificate.Void(Now);
        certificate.MakeFinal(1m, 1m, "EUR", Now);
        Assert.Equal((OperationsCertificateStatus.Final, (decimal?)640m, (decimal?)19.20m, "USD"), (certificate.Status, certificate.DeclaredValue, certificate.Premium, certificate.CurrencyCode));

        OperationsInsuranceCertificate dropped = OperationsInsuranceCertificate.Issue(Tenant, "IC26000002", Guid.NewGuid(), Guid.NewGuid(), plan, null, OperationsTransportMode.Air, Now, Now);
        dropped.Void(Now);
        Assert.Equal((OperationsCertificateStatus.Void, false), (dropped.Status, dropped.IsLive));
    }
}
