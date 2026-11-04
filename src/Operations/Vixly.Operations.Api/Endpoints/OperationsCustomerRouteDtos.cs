using Vixly.Contracts.Operations.Packages;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Api.Endpoints;

// =========================================================
// FILE: OperationsCustomerRouteDtos.cs
//
// PURPOSE:
// Turns destinations and insurance terms into what the customer site shows (Slice 4C).
//
// RESPONSIBILITIES:
// - A destination warehouse with the delivery it offers
// - The insurance terms in force for a mode, as names the customer site translates
//
// IMPORTANT NOTES:
// - Internal API only; enum values travel as their names
// =========================================================
internal static class OperationsCustomerRouteDtos
{
    public static CustomerDestinationDto Destination(OperationsWarehouse warehouse) =>
        new(warehouse.Id, warehouse.Name, warehouse.Address.City, warehouse.Address.CountryCode, warehouse.Offers(OperationsDeliveryModes.Pickup),
            warehouse.Offers(OperationsDeliveryModes.HomeDelivery), warehouse.HomeArea);

    // Null when the plan has no terms in force at that moment.
    public static CustomerInsuranceTermsDto? Insurance(OperationsInsurancePlan plan, DateTimeOffset at, string currency)
    {
        if (plan.VersionAt(at) is not { } version)
        {
            return null;
        }

        OperationsInsuranceCoverage coverage = version.Coverage;
        return new CustomerInsuranceTermsDto(plan.Mode.ToString(), coverage.Cover.ToString(), coverage.InsurerName, coverage.PolicyNumber,
            Flags(coverage.Risks, OperationsInsuredRisks.None), Flags(coverage.Exclusions, OperationsInsuranceExclusions.None, OperationsInsuranceExclusions.Standard), coverage.OtherExclusions,
            coverage.LiabilityPerUnit, coverage.LiabilityPerShipment, version.Method.ToString(), version.Percent, version.MinimumPremium, coverage.Payout.ToString(),
            coverage.DeductibleAmount, coverage.DeductiblePercent, coverage.MaxPerItem, version.MaxValue, coverage.DamageClaimDays, coverage.LossClaimDays,
            Flags(coverage.Proof, OperationsClaimProof.None), coverage.FiledBy.ToString(), coverage.TermsEn, coverage.TermsFr, coverage.TermsEs, currency,
            plan.Id, plan.Name, plan.PitchEn, plan.PitchFr, plan.PitchEs);
    }

    private static IReadOnlyList<string> Flags<T>(T value, params T[] skipped)
        where T : struct, Enum =>
        Enum.GetValues<T>().Where(flag => !skipped.Contains(flag) && value.HasFlag(flag)).Select(flag => flag.ToString()).ToList();
}
