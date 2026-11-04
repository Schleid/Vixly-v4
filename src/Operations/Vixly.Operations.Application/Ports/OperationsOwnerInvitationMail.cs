using Vixly.Shared.Kernel.Localization;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsOwnerInvitationMail.cs
//
// PURPOSE:
// The values of the owner invitation email.
//
// RESPONSIBILITIES:
// - Carry recipient, language, company and link
//
// IMPORTANT NOTES:
// - The link holds a single-use token; never logged
// =========================================================
public sealed record OperationsOwnerInvitationMail(string ToAddress, string ToName, LanguageCode Language, string CompanyName, string Link, int Hours);
