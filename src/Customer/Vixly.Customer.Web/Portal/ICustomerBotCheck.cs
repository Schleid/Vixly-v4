namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: ICustomerBotCheck.cs
//
// PURPOSE:
// Checks the bot-check answer of a customer form (ADR-0019).
//
// RESPONSIBILITIES:
// - Say whether the answer is a person
//
// IMPORTANT NOTES:
// - Replaced by a fake in tests
// =========================================================
public interface ICustomerBotCheck
{
    Task<bool> VerifyAsync(string? token, string? clientIp, CancellationToken cancellationToken);
}
