using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Events;

// =========================================================
// FILE: OperationsEventReason.cs
//
// PURPOSE:
// A company's own reason for a delay or an exception, in its words (Slice 6C, E-3).
//
// RESPONSIBILITIES:
// - Hold its kind and its wording in English, French and Spanish
// - Be switched off without changing the events that used it
//
// IMPORTANT NOTES:
// - Events keep the wording they were written with
// - English is required; the other languages fall back to it
// =========================================================
public sealed class OperationsEventReason : OperationsEntity
{
    public const int TextMaxLength = 80;

    public const int MaxPerCompany = 40;

    private OperationsEventReason()
    {
        (Code, TextEn) = (string.Empty, string.Empty);
    }

    private OperationsEventReason(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        (Code, TextEn) = (string.Empty, string.Empty);
    }

    public OperationsEventKind Kind { get; private set; }

    // c_ and twelve letters or digits, unique in the company.
    public string Code { get; private set; }

    public string TextEn { get; private set; }

    public string? TextFr { get; private set; }

    public string? TextEs { get; private set; }

    public bool IsActive { get; private set; }

    public static OperationsEventReason Create(Guid tenantId, OperationsEventKind kind, string? textEn, string? textFr, string? textEs, DateTimeOffset utcNow)
    {
        if (kind is not (OperationsEventKind.Delay or OperationsEventKind.Exception))
        {
            throw new DomainException(OperationsEventErrors.KindInvalid);
        }

        OperationsEventReason reason = new(tenantId, utcNow) { Kind = kind, Code = OperationsEventReasons.CustomPrefix + Guid.NewGuid().ToString("N")[..12], IsActive = true };
        reason.Change(textEn, textFr, textEs, true, utcNow);
        return reason;
    }

    public void Change(string? textEn, string? textFr, string? textEs, bool active, DateTimeOffset utcNow)
    {
        (TextEn, TextFr, TextEs, IsActive) = (Text(textEn) ?? throw new DomainException(OperationsEventErrors.ReasonRequired), Text(textFr), Text(textEs), active);
        MarkAsModified(utcNow);
    }

    public string In(string? language) => language switch
    {
        "fr" => TextFr ?? TextEn,
        "es" => TextEs ?? TextEn,
        _ => TextEn
    };

    public static string? Text(string? value)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return text is null || (text.Length <= TextMaxLength && !text.Any(char.IsControl) && text.IndexOfAny(['<', '>']) < 0)
            ? text
            : throw new DomainException(OperationsEventErrors.TextInvalid);
    }
}
