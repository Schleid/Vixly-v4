using Vixly.Shared.Kernel.Localization;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsTicketEscalatedMail.cs
//
// PURPOSE:
// The email to one manager: a ticket of their department waits too long (owner, 2026-10-29, ticket desk section 5).
//
// RESPONSIBILITIES:
// - Carry the ticket's topic, which step (nobody took it, or not resolved), when it was opened, and the link to it
//
// IMPORTANT NOTES:
// - Written in the manager's language with the company's name (never "Vixly"); no customer details beyond the ticket itself
// =========================================================
public sealed record OperationsTicketEscalatedMail(
    string ToAddress,
    string ToName,
    LanguageCode Language,
    string CompanyName,
    string Topic,
    int Step,
    string Reference,
    DateTimeOffset OpenedUtc,
    string Link);
