using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.EmailOperationsQuote;

// =========================================================
// FILE: EmailOperationsQuoteResponse.cs
//
// PURPOSE:
// The answer to emailing a quote: the address it went to (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class EmailOperationsQuoteResponse
{
    private EmailOperationsQuoteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string SentTo { get; private init; } = string.Empty;

    public string Number { get; private init; } = string.Empty;

    public static EmailOperationsQuoteResponse Success(string sentTo, string number) => new() { Succeeded = true, SentTo = sentTo, Number = number };

    public static EmailOperationsQuoteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new EmailOperationsQuoteResponse { Error = error };
    }
}
