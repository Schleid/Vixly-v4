using Vixly.Shared.Kernel.Errors;

namespace Vixly.Shared.Kernel.Domain;

// =========================================================
// FILE: DomainException.cs
//
// PURPOSE:
// Signals a business-rule or invariant violation raised by a
// domain object.
//
// RESPONSIBILITIES:
// - Carry the Error (stable code plus arguments) that
//   describes the violation
//
// IMPORTANT NOTES:
// - It carries a code, never a sentence. Text for people is
//   produced from the code in the presentation layer, in the
//   user's language (Global Standard 5.5, ADR-0005)
// - Message is the code and arguments, in English, for logs
//   only. It is never shown to a user (Global Standard S-9)
// - Use for domain and business errors only, never for
//   infrastructure or transport failures
// - Use cases translate it into a failure response; it must
//   not cross the API boundary as an unhandled exception
// =========================================================
public sealed class DomainException : Exception
{
    public DomainException(Error error)
        : base(error?.ToString() ?? throw new ArgumentNullException(nameof(error)))
    {
        Error = error;
    }

    public Error Error { get; }
}
