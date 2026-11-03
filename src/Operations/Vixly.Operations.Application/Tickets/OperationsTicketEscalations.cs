using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Application.Tickets;

// =========================================================
// FILE: OperationsTicketEscalations.cs
//
// PURPOSE:
// Finds the tickets that wait too long and has their managers told (owner, 2026-10-29, ticket desk section 5).
//
// RESPONSIBILITIES:
// - Step 1: nobody took the ticket within the company's hours; step 2: not resolved within its days
// - Each step once per ticket, recorded on the ticket; one outbox message per step
//
// IMPORTANT NOTES:
// - Limits from the company settings (defaults 4 hours and 3 days)
// - A job that missed a step sends only the latest one
// =========================================================
public sealed class OperationsTicketEscalations
{
    private const int Batch = 500;

    private readonly IOperationsTicketRepository _tickets;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsOutboxWriter _outbox;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public OperationsTicketEscalations(IOperationsTicketRepository tickets, IOperationsSettingsRepository settings, OperationsOutboxWriter outbox, IOperationsUnitOfWork unitOfWork)
    {
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<int> RunAsync(Guid tenantId, string portalSlug, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portalSlug);

        Vixly.Operations.Domain.Settings.OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        int hours = settings?.TicketTakeHours ?? OperationsTicket.TakeHoursDefault;
        int days = settings?.TicketResolveDays ?? OperationsTicket.ResolveDaysDefault;
        int sent = 0;

        foreach (OperationsTicket ticket in await _tickets.ListEscalatableAsync(utcNow.AddHours(-Math.Min(hours, days * 24)), Batch, cancellationToken))
        {
            int step = ticket.EscalationDue(hours, days, utcNow);

            if (step == 0)
            {
                continue;
            }

            ticket.Escalated(step, utcNow);
            _outbox.Write(OperationsTicketEscalatedNotice.MessageType, tenantId, ticket.Id, new OperationsTicketEscalatedNotice(ticket.Id, step, portalSlug));
            sent++;
        }

        return sent == 0 || await _unitOfWork.SaveChangesAsync(cancellationToken) ? sent : 0;
    }
}
