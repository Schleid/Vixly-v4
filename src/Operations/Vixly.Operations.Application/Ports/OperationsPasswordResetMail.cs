using Vixly.Shared.Kernel.Localization;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsPasswordResetMail.cs
//
// PURPOSE:
// The "reset your password" email (Slice 1C).
//
// RESPONSIBILITIES:
// - Carry the recipient, language, company and link
//
// IMPORTANT NOTES:
// - Sent only to the address of the account
// =========================================================
public sealed record OperationsPasswordResetMail(string ToAddress, string ToName, LanguageCode Language, string CompanyName, string Link, int Minutes);
