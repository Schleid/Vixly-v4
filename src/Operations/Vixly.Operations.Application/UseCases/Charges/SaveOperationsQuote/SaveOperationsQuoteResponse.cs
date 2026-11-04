using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsQuote;

// =========================================================
// FILE: SaveOperationsQuoteResponse.cs
//
// PURPOSE:
// Saves a quote for a customer on one service to a zone, with an optional preferred price (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsQuoteResponse
{
    private SaveOperationsQuoteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid QuoteId { get; private init; }

    public string Number { get; private init; } = string.Empty;

    public static SaveOperationsQuoteResponse Success(Guid quoteId, string number)
    {
        return new SaveOperationsQuoteResponse { Succeeded = true, QuoteId = quoteId, Number = number };
    }

    public static SaveOperationsQuoteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsQuoteResponse { Error = error };
    }
}
