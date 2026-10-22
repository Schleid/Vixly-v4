using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Resources;

namespace Vixly.Operations.Web.Components.Charges;

// =========================================================
// FILE: OperationsInsuranceWords.cs
//
// PURPOSE:
// An insurance product in a few words: its price rule and what it covers, for the prices page, the counter and the page to sign (Slice 5D).
//
// RESPONSIBILITIES:
// - Write the price rule: "3% of the declared value, at least 5.00 USD"
// - Write what the product covers from its terms: risks, who pays, deductible, most per item, days to claim
// - Give the company's pitch in the page's language when it wrote one
//
// IMPORTANT NOTES:
// - Text comes from OperationsText (L-10); the generated words never contradict the terms, the pitch is the company's own
// =========================================================
public static class OperationsInsuranceWords
{
    public static string Rule(IStringLocalizer<OperationsText> text, OperationsInsuranceVersion terms, string currency)
    {
        ArgumentNullException.ThrowIfNull(terms);
        string rule = terms.Method == OperationsInsuranceMethod.Percent
            ? OperationsChargeWords.Text(text, terms.MinimumPremium is > 0 ? "ui.insurance_choice.rule.percent_min" : "ui.insurance_choice.rule.percent",
                ("percent", OperationsChargeWords.Number(terms.Percent)), ("minimum", OperationsChargeWords.Money(terms.MinimumPremium ?? 0m, currency)))
            : text["ui.insurance_choice.rule.bands"].Value;
        return terms.MaxValue is { } max ? rule + " · " + OperationsChargeWords.Text(text, "ui.insurance_choice.rule.max", ("amount", OperationsChargeWords.Money(max, currency))) : rule;
    }

    // What it covers, one line each, from the terms.
    public static IReadOnlyList<string> Covers(IStringLocalizer<OperationsText> text, OperationsInsuranceVersion terms, string currency)
    {
        ArgumentNullException.ThrowIfNull(terms);
        OperationsInsuranceCoverage coverage = terms.Coverage;
        List<string> lines = new();
        string[] risks = Enum.GetValues<OperationsInsuredRisks>().Where(risk => risk != OperationsInsuredRisks.None && coverage.Risks.HasFlag(risk))
            .Select(risk => text["ui.insurance.risk." + risk].Value).ToArray();

        if (risks.Length > 0)
        {
            lines.Add(OperationsChargeWords.Text(text, "ui.insurance_choice.covers", ("risks", string.Join(", ", risks))));
        }

        lines.Add(coverage.Cover == OperationsInsuranceCover.Insurer && coverage.InsurerName is { } insurer
            ? OperationsChargeWords.Text(text, "ui.insurance_choice.insurer", ("insurer", insurer))
            : text["ui.insurance_choice.own"].Value);

        if (coverage.DeductibleAmount is > 0 and { } amount)
        {
            lines.Add(OperationsChargeWords.Text(text, "ui.insurance_choice.deductible", ("amount", OperationsChargeWords.Money(amount, currency))));
        }
        else if (coverage.DeductiblePercent is > 0 and { } percent)
        {
            lines.Add(OperationsChargeWords.Text(text, "ui.insurance_choice.deductible", ("amount", percent.ToString("0.###", CultureInfo.CurrentCulture) + " %")));
        }

        if (coverage.MaxPerItem is { } perItem)
        {
            lines.Add(OperationsChargeWords.Text(text, "ui.insurance_choice.per_item", ("amount", OperationsChargeWords.Money(perItem, currency))));
        }

        lines.Add(OperationsChargeWords.Text(text, "ui.insurance_choice.claim_days", ("damage", coverage.DamageClaimDays), ("loss", coverage.LossClaimDays)));
        return lines;
    }

    public static string? Pitch(OperationsInsurancePlan product)
    {
        ArgumentNullException.ThrowIfNull(product);
        return product.Pitch(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }
}
