namespace Vixly.Platform.Application.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedAnswer.cs
//
// PURPOSE:
// TechOS's answer to one batch.
//
// RESPONSIBILITIES:
// - Carry the outcome, a short code for staff and the per-event lists of a 200 answer
//
// IMPORTANT NOTES:
// - Code is "http_<status>" or "no_answer"; never a secret or a body
// =========================================================
public sealed record PlatformSalesFeedAnswer(
    PlatformSalesFeedAnswerKind Kind,
    string Code,
    IReadOnlyList<Guid> Accepted,
    IReadOnlyList<Guid> Duplicates,
    IReadOnlyList<PlatformSalesFeedRejection> Rejected)
{
    public static PlatformSalesFeedAnswer Of(PlatformSalesFeedAnswerKind kind, string code)
    {
        return new PlatformSalesFeedAnswer(kind, code, Array.Empty<Guid>(), Array.Empty<Guid>(), Array.Empty<PlatformSalesFeedRejection>());
    }
}
