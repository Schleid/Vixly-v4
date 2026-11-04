using Vixly.Operations.Application.Events;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Events;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Events.FindOperationsEventSubject;

// =========================================================
// FILE: FindOperationsEventSubjectUseCase.cs
//
// PURPOSE:
// Find what a number typed in the events workspace is (Slice 6C): a parcel, a shipment, a bundle or a dispatch.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.events.view or ops.events.add, at the warehouse the subject leaves from or goes to (E-6)
// - A carrier tracking number is matched as typed too
// =========================================================
public sealed class FindOperationsEventSubjectUseCase
{
    private readonly OperationsEventDesk _desk;

    private readonly IOperationsEventRepository _events;

    public FindOperationsEventSubjectUseCase(
        OperationsEventDesk desk,
        IOperationsEventRepository events)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public async Task<FindOperationsEventSubjectResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        FindOperationsEventSubjectRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.EventsView, cancellationToken);

        if (refused is not null)
        {
            return FindOperationsEventSubjectResponse.Failure(refused);
        }

        string? query = request.Query?.Trim();

        if (string.IsNullOrEmpty(query) || query.Length > 64)
        {
            return FindOperationsEventSubjectResponse.Failure(OperationsEventErrors.NotFound);
        }

        // The first match the person's warehouses cover: a tracking number reused at another warehouse never hides theirs (review).
        List<Guid> mine = warehouses.Select(item => item.Id).ToList();

        foreach ((OperationsTrackingSubject kind, Guid id) in (await _events.FindSubjectsAsync(query, cancellationToken))
            .Concat(query == query.ToUpperInvariant() ? Array.Empty<(OperationsTrackingSubject, Guid)>() : await _events.FindSubjectsAsync(query.ToUpperInvariant(), cancellationToken)))
        {
            if (await _desk.SubjectAsync(kind, id, cancellationToken) is { } subject && subject.Covers(mine))
            {
                return FindOperationsEventSubjectResponse.Success(subject.Kind, subject.Id);
            }
        }

        return FindOperationsEventSubjectResponse.Failure(OperationsEventErrors.NotFound);
    }
}
