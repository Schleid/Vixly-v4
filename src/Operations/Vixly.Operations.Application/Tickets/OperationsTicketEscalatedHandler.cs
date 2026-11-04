using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Messaging;
using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Application.Tickets;

// =========================================================
// FILE: OperationsTicketEscalatedHandler.cs
//
// PURPOSE:
// Sends the escalation of a ticket to the managers of its department (owner, 2026-10-29, ticket desk section 5).
//
// RESPONSIBILITIES:
// - Load the ticket, its managers (OperationsTicketPeople) and the company's name
// - Send each one the topic, the step, when it was opened and the link, in their language
//
// IMPORTANT NOTES:
// - A ticket resolved or taken in the meantime (step 1) sends nothing
// - Step 2 also goes to the Owner
// =========================================================
public sealed class OperationsTicketEscalatedHandler : IOperationsOutboxHandler
{
    private readonly IOperationsTicketRepository _tickets;

    private readonly OperationsTicketPeople _people;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsIdentityMailer _mailer;

    private readonly IOperationsPortalLinks _links;

    public OperationsTicketEscalatedHandler(IOperationsTicketRepository tickets, OperationsTicketPeople people, IOperationsBrandRepository brands, IOperationsIdentityMailer mailer,
        IOperationsPortalLinks links)
    {
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _people = people ?? throw new ArgumentNullException(nameof(people));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _links = links ?? throw new ArgumentNullException(nameof(links));
    }

    public string MessageType => OperationsTicketEscalatedNotice.MessageType;

    public async Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        OperationsTicketEscalatedNotice notice = OperationsOutboxWriter.Read<OperationsTicketEscalatedNotice>(message);

        if (await _tickets.GetAsync(notice.TicketId, cancellationToken) is not { } ticket || ticket.Status == OperationsTicketStatus.Resolved
            || (notice.Step == 1 && ticket.TakenByUserId is not null) || string.IsNullOrWhiteSpace(notice.PortalSlug))
        {
            return;
        }

        string company = (await _brands.FindDisplayNameAsync(cancellationToken)) ?? string.Empty;
        string link = _links.Page(notice.PortalSlug, "/staff/tickets/" + ticket.Id);

        foreach (OperationsUser manager in await _people.ManagersAsync(ticket.Topic, notice.Step >= 2, cancellationToken))
        {
            await _mailer.SendTicketEscalatedAsync(new OperationsTicketEscalatedMail(manager.Email.Value, manager.FullName, manager.PreferredLanguage, company, ticket.Topic.ToString(),
                notice.Step, ticket.TrackingNumber, ticket.CreatedUtc, link), cancellationToken);
        }
    }
}
