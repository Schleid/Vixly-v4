using Vixly.Shared.Kernel.Localization;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsCustomerMail.cs
//
// PURPOSE:
// One customer account email, in the customer language and the tenant brand (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the recipient, the company name and the link
//
// IMPORTANT NOTES:
// - Never says "Vixly" (W-1)
// =========================================================
public sealed record OperationsCustomerMail(OperationsCustomerMailKind Kind, string ToAddress, string ToName, LanguageCode Language, string CompanyName, string Link);
