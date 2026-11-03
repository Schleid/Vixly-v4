using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.QueryOperationsApi;

// =========================================================
// FILE: QueryOperationsApiResponse.cs
//
// PURPOSE:
// The answer to queryOperationsApi.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class QueryOperationsApiResponse
{
    private QueryOperationsApiResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public object? Item { get; private init; }

    public IReadOnlyList<object> Items { get; private init; } = Array.Empty<object>();

    public string? NextCursor { get; private init; }

    public static QueryOperationsApiResponse Success(object? item, IReadOnlyList<object> items, string? nextCursor)
    {
        return new QueryOperationsApiResponse { Succeeded = true, Item = item, Items = items, NextCursor = nextCursor };
    }

    public static QueryOperationsApiResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new QueryOperationsApiResponse { Error = error };
    }
}
