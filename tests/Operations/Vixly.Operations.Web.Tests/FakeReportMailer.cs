using System.Collections.Concurrent;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: FakeReportMailer.cs
//
// PURPOSE:
// Stands in for the email of scheduled reports (Slice 11): keeps each mail and its file.
//
// RESPONSIBILITIES:
// - Keep what would have been sent
//
// IMPORTANT NOTES:
// - Tests only; never reaches the network
// =========================================================
internal sealed class FakeReportMailer : IOperationsReportMailer
{
    public ConcurrentQueue<OperationsReportMail> Sent { get; } = new();

    public Task SendAsync(OperationsReportMail mail, CancellationToken cancellationToken)
    {
        Sent.Enqueue(mail);
        return Task.CompletedTask;
    }
}
