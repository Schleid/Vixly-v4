using Microsoft.AspNetCore.Http;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Web.Components.Charges;

// =========================================================
// FILE: OperationsInsuranceForm.cs
//
// PURPOSE:
// Reads the insurance terms typed on the Prices page (Slice 4C section 8).
//
// RESPONSIBILITIES:
// - Turn the form fields into OperationsInsuranceCoverageTerms, or null when a value cannot be read
//
// IMPORTANT NOTES:
// - The domain checks the values (OperationsInsuranceCoverage)
// =========================================================
public static class OperationsInsuranceForm
{
    public static OperationsInsuranceCoverageTerms? Read(IFormCollection form)
    {
        ArgumentNullException.ThrowIfNull(form);

        if (!Enum.TryParse(Text(form, "cover"), false, out OperationsInsuranceCover cover) || !Enum.TryParse(Text(form, "payout"), false, out OperationsInsurancePayout payout)
            || !Enum.TryParse(Text(form, "filed_by"), false, out OperationsClaimFiler filedBy)
            || !int.TryParse(Text(form, "damage_days"), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int damageDays)
            || !int.TryParse(Text(form, "loss_days"), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int lossDays)
            || !Flags(form, "risks", out OperationsInsuredRisks risks) || !Flags(form, "exclusions", out OperationsInsuranceExclusions exclusions)
            || !Flags(form, "proof", out OperationsClaimProof proof))
        {
            return null;
        }

        string[] typed = { Text(form, "liability_per_unit"), Text(form, "liability_per_shipment"), Text(form, "deductible_amount"), Text(form, "deductible_percent"), Text(form, "max_per_item") };
        decimal?[] amounts = typed.Select(value => value.Length == 0 ? null : OperationsPriceText.Number(value)).ToArray();

        return typed.Zip(amounts).Any(pair => pair.First.Length > 0 && pair.Second is null)
            ? null
            : new OperationsInsuranceCoverageTerms(cover, Optional(form, "insurer_name"), Optional(form, "policy_number"), Optional(form, "insurer_contact"), risks, exclusions,
                Optional(form, "other_exclusions"), amounts[0], amounts[1], payout, amounts[2], amounts[3], amounts[4], damageDays, lossDays, proof, filedBy,
                Optional(form, "terms_en"), Optional(form, "terms_fr"), Optional(form, "terms_es"));
    }

    // Checkboxes of one flags enum, by name; a value that is not one of its names is refused.
    private static bool Flags<T>(IFormCollection form, string key, out T value)
        where T : struct, Enum
    {
        int combined = 0;
        value = default;

        foreach (string? name in form[key].Take(20))
        {
            if (!Enum.TryParse(name, false, out T flag) || !Enum.IsDefined(flag) || (name ?? string.Empty).Any(char.IsDigit))
            {
                return false;
            }

            combined |= Convert.ToInt32(flag, System.Globalization.CultureInfo.InvariantCulture);
        }

        value = (T)Enum.ToObject(typeof(T), combined);
        return true;
    }

    private static string Text(IFormCollection form, string key) => form[key].ToString().Trim();

    private static string? Optional(IFormCollection form, string key) => Text(form, key) is { Length: > 0 } text ? text : null;
}
