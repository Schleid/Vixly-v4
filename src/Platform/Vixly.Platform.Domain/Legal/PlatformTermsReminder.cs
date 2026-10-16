using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Legal;

// =========================================================
// FILE: PlatformTermsReminder.cs
//
// PURPOSE:
// One Terms reminder sent to a tenant (D-Lg7).
//
// RESPONSIBILITIES:
// - Record the step, the language and, for manual ones, the staff member and reason
//
// IMPORTANT NOTES:
// - Automatic steps are unique per tenant and version (filtered index)
// - Delivery goes through the outbox
// =========================================================
public sealed class PlatformTermsReminder : AuditableEntity
{
    private PlatformTermsReminder()
    {
        Language = string.Empty;
    }

    private PlatformTermsReminder(Guid tenantId, Guid documentId, PlatformTermsReminderStep step, string language, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformTenantId = tenantId;
        PlatformLegalDocumentId = documentId;
        Step = step;
        Language = language;
    }

    public Guid PlatformTenantId { get; private set; }

    public Guid PlatformLegalDocumentId { get; private set; }

    public PlatformTermsReminderStep Step { get; private set; }

    public string Language { get; private set; }

    public Guid? SentByUserId { get; private set; }

    public string? Reason { get; private set; }

    public static PlatformTermsReminder Automatic(Guid tenantId, Guid documentId, PlatformTermsReminderStep step, string language, DateTimeOffset utcNow)
    {
        if (step == PlatformTermsReminderStep.Manual || !Enum.IsDefined(step))
        {
            throw new ArgumentOutOfRangeException(nameof(step));
        }

        return new PlatformTermsReminder(tenantId, documentId, step, language, utcNow);
    }

    public static PlatformTermsReminder Manual(Guid tenantId, Guid documentId, string language, string? reason, Guid staffUserId, DateTimeOffset? lastManualUtc, DateTimeOffset utcNow)
    {
        if (staffUserId == Guid.Empty)
        {
            throw new DomainException(PlatformLegalErrors.ActorRequired);
        }

        string text = reason?.Trim() ?? string.Empty;

        if (text.Length == 0 || text.Length > PlatformLegalLimits.ReasonMaxLength)
        {
            throw new DomainException(PlatformLegalErrors.ReasonRequired);
        }

        if (lastManualUtc is { } last && utcNow - last < PlatformLegalLimits.ManualReminderCooldown)
        {
            throw new DomainException(PlatformLegalErrors.ReminderTooSoon);
        }

        return new PlatformTermsReminder(tenantId, documentId, PlatformTermsReminderStep.Manual, language, utcNow) { SentByUserId = staffUserId, Reason = text };
    }
}
