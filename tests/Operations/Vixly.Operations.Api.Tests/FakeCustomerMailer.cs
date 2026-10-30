using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Api.Tests;

// =========================================================
// FILE: FakeCustomerMailer.cs
//
// PURPOSE:
// Keeps the customer emails instead of sending them (Slice 2).
//
// RESPONSIBILITIES:
// - Remember every email; read the token of the last link
//
// IMPORTANT NOTES:
// - Test-only
// =========================================================
internal sealed class FakeCustomerMailer : IOperationsCustomerMailer
{
    public List<OperationsCustomerMail> Sent { get; } = new();

    public Task SendAsync(OperationsCustomerMail mail, CancellationToken cancellationToken)
    {
        lock (Sent)
        {
            Sent.Add(mail);
        }

        return Task.CompletedTask;
    }

    public string LastToken(string address) =>
        Uri.UnescapeDataString(Sent.Last(mail => mail.ToAddress == address).Link.Split("token=")[1]);
}
