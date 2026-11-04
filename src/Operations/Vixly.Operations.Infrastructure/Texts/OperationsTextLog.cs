using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Texts;
using Vixly.Operations.Infrastructure.Persistence;

namespace Vixly.Operations.Infrastructure.Texts;

// =========================================================
// FILE: OperationsTextLog.cs
//
// PURPOSE:
// Keeps a line of the company's text log, and a customer's STOP, in a scope of their own (Slice 9; review).
//
// RESPONSIBILITIES:
// - Save the line apart from the caller's database context: a failure here never clears or commits the caller's work
// - On STOP, take the customer's notices back to email (a direct update: no clash with the caller's copy) and keep it on the audit trail
//
// IMPORTANT NOTES:
// - Never throws; a lost line is logged as a warning without the number
// =========================================================
internal sealed class OperationsTextLog : IOperationsTextLog
{
    private readonly IServiceScopeFactory _scopes;

    private readonly ILogger<OperationsTextLog> _logger;

    public OperationsTextLog(IServiceScopeFactory scopes, ILogger<OperationsTextLog> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RecordAsync(OperationsTextMessage message, bool optedOut, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(message.TenantId);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            context.TextMessages.Add(message);

            if (optedOut)
            {
                DateTimeOffset utcNow = message.CreatedUtc;
                await context.Customers.Where(customer => customer.Id == message.CustomerId && customer.NoticeChannel != OperationsNoticeChannel.Email)
                    .ExecuteUpdateAsync(set => set.SetProperty(customer => customer.NoticeChannel, OperationsNoticeChannel.Email).SetProperty(customer => customer.NoticeChannelUtc, utcNow),
                        CancellationToken.None);
                scope.ServiceProvider.GetRequiredService<OperationsAuditTrail>().Record(null, "ops.customers.texts_stopped", nameof(OperationsCustomer), message.CustomerId,
                    before: new { channel = message.Channel.ToString() });
            }

            // The text has gone: its line is kept even when the caller's request is being cancelled.
            await context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "A text message line could not be kept ({Status}).", message.Status);
        }
    }
}
