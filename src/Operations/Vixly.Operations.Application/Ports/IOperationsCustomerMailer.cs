namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsCustomerMailer.cs
//
// PURPOSE:
// Sends customer account emails (Slice 2).
//
// RESPONSIBILITIES:
// - Send one email of a kind
//
// IMPORTANT NOTES:
// - Tenant branded
// =========================================================
public interface IOperationsCustomerMailer
{
    Task SendAsync(OperationsCustomerMail mail, CancellationToken cancellationToken);
}
