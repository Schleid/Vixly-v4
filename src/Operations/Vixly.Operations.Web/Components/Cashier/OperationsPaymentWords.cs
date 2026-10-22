using Microsoft.Extensions.Localization;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Resources;

namespace Vixly.Operations.Web.Components.Cashier;

// =========================================================
// FILE: OperationsPaymentWords.cs
//
// PURPOSE:
// Words of the cashier pages (Slice 5F step 2).
//
// RESPONSIBILITIES:
// - Name a payment method, with the company's own name for its other methods
// - Name an invoice's payment state
//
// IMPORTANT NOTES:
// - Text comes from OperationsText (L-10)
// =========================================================
public static class OperationsPaymentWords
{
    public static string Method(IStringLocalizer<OperationsText> text, OperationsPaymentMethod method, IReadOnlyDictionary<OperationsPaymentMethod, string>? others) =>
        others is not null && others.TryGetValue(method, out string? name) ? name : (text ?? throw new ArgumentNullException(nameof(text)))["ui.payments.method." + method].Value;

    public static string State(OperationsPaymentState state) => state switch
    {
        OperationsPaymentState.Paid => "paid",
        OperationsPaymentState.AwaitingConfirmation => "awaiting",
        OperationsPaymentState.Voided => "voided",
        OperationsPaymentState.OnAccount => "on_account",
        OperationsPaymentState.AwaitingCredit => "awaiting_credit",
        _ => "unpaid"
    };
}
