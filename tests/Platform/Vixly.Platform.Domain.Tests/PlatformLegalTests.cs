using System.Text;
using Vixly.Platform.Domain.Legal;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformLegalTests.cs
//
// PURPOSE:
// Proves the Slice 8 legal rules: drafts and publishing, files, acceptances, deadlines and reminders.
//
// RESPONSIBILITIES:
// - Publishing needs every language, the included versions and a 30-day notice for required Terms
// - Files must be real PDFs; the hash is made by Platform
// - Only the owner accepts; staff records need a reason and evidence
// - Deadline, status and the reminder step due
//
// IMPORTANT NOTES:
// - Tests assert codes, never text (L-1)
// =========================================================
public sealed class PlatformLegalTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Staff = Guid.NewGuid();

    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n");

    private static string Code(Action action) => Assert.Throws<DomainException>(action).Error.Code;

    private static PlatformLegalDocument Published(PlatformLegalDocumentKind kind, string version = "2026-10")
    {
        PlatformLegalDocument document = PlatformLegalDocument.CreateDraft(kind, version, Staff, Now);

        if (kind == PlatformLegalDocumentKind.Terms)
        {
            document.SetParts(new[] { Published(PlatformLegalDocumentKind.Privacy), Published(PlatformLegalDocumentKind.DataProcessing) }, Now);
        }

        document.Publish(new[] { "en", "fr", "es" }, Array.Empty<PlatformLegalDocumentKind>(), Staff, Now);
        return document;
    }

    [Fact]
    public void Terms_PublishWithEveryLanguage_TheIncludedVersions_AndThirtyDaysNotice()
    {
        PlatformLegalDocument privacy = Published(PlatformLegalDocumentKind.Privacy);
        PlatformLegalDocument dpa = Published(PlatformLegalDocumentKind.DataProcessing);
        PlatformLegalDocument terms = PlatformLegalDocument.CreateDraft(PlatformLegalDocumentKind.Terms, "2026-11", Staff, Now);

        Assert.Equal("platform.legal.version_invalid", Code(() => PlatformLegalDocument.CreateDraft(PlatformLegalDocumentKind.Terms, "has space", Staff, Now)));
        Assert.Equal("platform.legal.languages_missing", Code(() => terms.Publish(new[] { "en", "fr" }, Array.Empty<PlatformLegalDocumentKind>(), Staff, Now)));
        Assert.Equal("platform.legal.parts_invalid", Code(() => terms.Publish(new[] { "en", "fr", "es" }, Array.Empty<PlatformLegalDocumentKind>(), Staff, Now)));
        Assert.Equal("platform.legal.parts_invalid", Code(() => terms.SetParts(new[] { privacy, PlatformLegalDocument.CreateDraft(PlatformLegalDocumentKind.ServiceLevel, "1", Staff, Now) }, Now)));

        terms.SetParts(new[] { privacy, dpa }, Now);
        terms.UpdateDraft("2026-11", true, Now.AddDays(10), Now);
        Assert.Equal("platform.legal.effective_date_invalid", Code(() => terms.Publish(new[] { "en", "fr", "es" }, new[] { PlatformLegalDocumentKind.Privacy, PlatformLegalDocumentKind.DataProcessing }, Staff, Now)));

        terms.UpdateDraft("2026-11", true, null, Now);
        terms.Publish(new[] { "en", "fr", "es" }, new[] { PlatformLegalDocumentKind.Privacy, PlatformLegalDocumentKind.DataProcessing }, Staff, Now);
        Assert.Equal(PlatformLegalDocumentStatus.Published, terms.Status);
        Assert.Equal(Now.AddDays(30), terms.DeadlineUtc);
        Assert.Equal(2, terms.Parts.Count);
        Assert.Equal("platform.legal.not_draft", Code(() => terms.UpdateDraft("x", false, null, Now)));

        PlatformLegalDocument notice = PlatformLegalDocument.CreateDraft(PlatformLegalDocumentKind.Terms, "2026-11b", Staff, Now);
        notice.SetParts(new[] { privacy, dpa }, Now);
        notice.UpdateDraft("2026-11b", false, null, Now);
        notice.Publish(new[] { "en", "fr", "es" }, Array.Empty<PlatformLegalDocumentKind>(), Staff, Now);
        Assert.False(notice.NeedsAcceptance);
        Assert.Null(notice.DeadlineUtc);
    }

    [Fact]
    public void File_MustBeARealPdf_AndPlatformMakesTheHash()
    {
        Guid document = Guid.NewGuid();
        Assert.Equal("platform.legal.file_invalid", Code(() => PlatformLegalDocumentFile.Create(document, "en", Encoding.ASCII.GetBytes("<html>"), null, Now)));
        Assert.Equal("platform.legal.languages_missing", Code(() => PlatformLegalDocumentFile.Create(document, "de", Pdf, null, Now)));
        Assert.Equal("platform.legal.summary_invalid", Code(() => PlatformLegalDocumentFile.Create(document, "en", Pdf, new string('a', 2001), Now)));

        PlatformLegalDocumentFile file = PlatformLegalDocumentFile.Create(document, "fr", Pdf, "  Nouvelle clause  ", Now);
        Assert.Equal(64, file.Sha256.Length);
        Assert.Equal("Nouvelle clause", file.Summary);
        Assert.Equal(Pdf.Length, file.SizeBytes);
    }

    [Fact]
    public void Acceptance_OwnerOnly_AndStaffRecordsNeedAReasonAndEvidence()
    {
        PlatformLegalDocument terms = Published(PlatformLegalDocumentKind.Terms);
        PlatformLegalDocumentFile file = PlatformLegalDocumentFile.Create(terms.Id, "es", Pdf, null, Now);
        Guid tenant = Guid.NewGuid();

        Assert.Equal("platform.legal.owner_only", Code(() => PlatformTenantTermsAcceptance.ByOwner(tenant, terms, file, "u-1", "Ana", "admin", "203.0.113.9", "Edge", Now)));
        PlatformTenantTermsAcceptance accepted = PlatformTenantTermsAcceptance.ByOwner(tenant, terms, file, "u-1", "Ana", "Owner", "203.0.113.9", "Edge", Now);
        Assert.Equal("es", accepted.Language);
        Assert.Equal(file.Sha256, accepted.Sha256);
        Assert.Equal(PlatformTermsAcceptanceSource.Operations, accepted.Source);

        Assert.Equal("platform.legal.reason_required", Code(() => PlatformTenantTermsAcceptance.RecordedByStaff(tenant, terms, file, "Ana", " ", Pdf, "application/pdf", Staff, Now)));
        Assert.Equal("platform.legal.evidence_invalid", Code(() => PlatformTenantTermsAcceptance.RecordedByStaff(tenant, terms, file, "Ana", "Signed paper", Pdf, "text/plain", Staff, Now)));
        Assert.Equal(PlatformTermsAcceptanceSource.StaffRecord, PlatformTenantTermsAcceptance.RecordedByStaff(tenant, terms, file, "Ana", "Signed paper", Pdf, "application/pdf", Staff, Now).Source);

        PlatformLegalDocument draft = PlatformLegalDocument.CreateDraft(PlatformLegalDocumentKind.Terms, "next", Staff, Now);
        Assert.Equal("platform.legal.nothing_to_accept", Code(() => PlatformTenantTermsAcceptance.ByOwner(tenant, draft, PlatformLegalDocumentFile.Create(draft.Id, "en", Pdf, null, Now), "u", "A", "owner", null, null, Now)));
    }

    [Fact]
    public void Deadline_GivesNewTenantsThirtyDays_AndStatusTurnsOverdue()
    {
        PlatformLegalDocument terms = PlatformLegalDocument.CreateDraft(PlatformLegalDocumentKind.Terms, "v1", Staff, Now);
        terms.SetParts(new[] { Published(PlatformLegalDocumentKind.Privacy), Published(PlatformLegalDocumentKind.DataProcessing) }, Now);
        terms.Publish(new[] { "en", "fr", "es" }, Array.Empty<PlatformLegalDocumentKind>(), Staff, Now);

        Assert.Equal(Now.AddDays(30), PlatformLegalRules.Deadline(terms, Now.AddYears(-1)));
        Assert.Equal(Now.AddDays(80), PlatformLegalRules.Deadline(terms, Now.AddDays(50)));
        Assert.Equal(PlatformLegalStatus.Due, PlatformLegalRules.Status(true, false, Now.AddDays(30), Now));
        Assert.Equal(PlatformLegalStatus.Overdue, PlatformLegalRules.Status(true, false, Now.AddDays(30), Now.AddDays(30)));
        Assert.Equal(PlatformLegalStatus.Accepted, PlatformLegalRules.Status(true, true, Now.AddDays(30), Now.AddDays(40)));
        Assert.Equal(PlatformLegalStatus.NotRequired, PlatformLegalRules.Status(false, false, Now, Now));
    }

    [Fact]
    public void Reminders_FollowTheSteps_SendOnlyTheLatestAfterDowntime_AndManualOnesWaitADay()
    {
        DateTimeOffset deadline = Now.AddDays(30);
        PlatformTermsReminderStep[] none = Array.Empty<PlatformTermsReminderStep>();

        Assert.Equal(PlatformTermsReminderStep.Published, PlatformLegalRules.DueStep(Now, deadline, none, Now));
        Assert.Null(PlatformLegalRules.DueStep(Now, deadline, new[] { PlatformTermsReminderStep.Published }, Now.AddDays(10)));
        Assert.Equal(PlatformTermsReminderStep.Before14Days, PlatformLegalRules.DueStep(Now, deadline, new[] { PlatformTermsReminderStep.Published }, Now.AddDays(16)));
        Assert.Equal(PlatformTermsReminderStep.Before1Day, PlatformLegalRules.DueStep(Now, deadline, new[] { PlatformTermsReminderStep.Published }, Now.AddDays(29.5)));
        Assert.Equal(PlatformTermsReminderStep.After4Weeks, PlatformLegalRules.DueStep(Now, deadline, new[] { PlatformTermsReminderStep.After3Weeks }, Now.AddDays(60)));
        Assert.Null(PlatformLegalRules.DueStep(Now, deadline, new[] { PlatformTermsReminderStep.After4Weeks }, Now.AddDays(90)));

        PlatformTermsReminder.Manual(Guid.NewGuid(), Guid.NewGuid(), "en", "Called the owner", Staff, null, Now);
        Assert.Equal("platform.legal.reminder_too_soon", Code(() => PlatformTermsReminder.Manual(Guid.NewGuid(), Guid.NewGuid(), "en", "Again", Staff, Now.AddHours(-2), Now)));
    }
}
