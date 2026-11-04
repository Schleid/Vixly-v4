using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.QueryOperationsApi;

// =========================================================
// FILE: QueryOperationsApiUseCase.cs
//
// PURPOSE:
// Reads the company’s data for its API (Slice 10): a page of a resource, or one record.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Read only; oldest first; at most 100 a page, with a cursor to the next
// - No signed-in person: the key was checked for this tenant before
// =========================================================
public sealed class QueryOperationsApiUseCase
{
    private readonly IOperationsApiReader _reader;

    public QueryOperationsApiUseCase(
        IOperationsApiReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<QueryOperationsApiResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        QueryOperationsApiRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // Called only after the key was checked (AuthenticateOperationsApiKey) for this tenant.
        if (context.TenantId is null || !Enum.IsDefined(request.Resource))
        {
            return QueryOperationsApiResponse.Failure(OperationsCommonErrors.NotFound);
        }

        if (request.Id is { } id)
        {
            return request.Resource != OperationsApiResource.Tracking && await _reader.GetAsync(request.Resource, id, cancellationToken) is { } item
                ? QueryOperationsApiResponse.Success(item, Array.Empty<object>(), null)
                : QueryOperationsApiResponse.Failure(OperationsCommonErrors.NotFound);
        }

        int limit = Math.Clamp(request.Limit, 1, 100);
        (DateTimeOffset afterUtc, Guid afterId) = OperationsApiCursor.TryRead(request.Cursor, out DateTimeOffset utc, out Guid last) ? (utc, last) : (DateTimeOffset.MinValue, Guid.Empty);

        // One more than asked: tells whether there is a next page.
        IReadOnlyList<OperationsApiRow> rows = await _reader.ListAsync(request.Resource, new OperationsApiQuery(afterUtc, afterId, limit + 1)
        {
            CustomerId = request.CustomerId,
            ShipmentId = request.ShipmentId,
            SubjectId = request.SubjectId,
            SubjectResource = request.SubjectResource
        }, cancellationToken);
        List<OperationsApiRow> page = rows.Take(limit).ToList();
        string? next = rows.Count > limit ? OperationsApiCursor.Write(page[^1].CreatedUtc, page[^1].Id) : null;
        return QueryOperationsApiResponse.Success(null, page.Select(row => row.View).ToList(), next);
    }
}
