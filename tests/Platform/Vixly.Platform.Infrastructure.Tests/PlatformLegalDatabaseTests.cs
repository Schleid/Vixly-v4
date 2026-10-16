using System.Text;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Queries;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformLegalDatabaseTests.cs
//
// PURPOSE:
// Proves the Slice 8 mapping against SQL Server: documents, files, acceptances, reminders and the register.
//
// RESPONSIBILITIES:
// - Round-trip a published Terms version with its parts and files
// - One published version per kind; one acceptance per tenant and version; each automatic reminder once
// - The register and the reminder list work out status and deadline in SQL
//
// IMPORTANT NOTES:
// - Needs VIXLY_TEST_SQL_SERVER; skipped otherwise
// - Legal documents are shared by the whole database, so each test uses its own version labels
// =========================================================
public sealed class PlatformLegalDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n% db test\n");

    private readonly PlatformDatabaseFixture _database;

    public PlatformLegalDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task TermsWithPartsAndFiles_RoundTrip_TheRegisterAndReminderListWork_AndUniqueRulesHold()
    {
        Guid actor = _database.ActorUserId == Guid.Empty ? Guid.NewGuid() : _database.ActorUserId;
        string tag = Guid.NewGuid().ToString("N")[..8];
        PlatformTenant accepted = NewTenant(actor);
        PlatformTenant waiting = NewTenant(actor);
        PlatformLegalDocument privacy = PlatformLegalDocument.CreateDraft(PlatformLegalDocumentKind.Privacy, "p-" + tag, actor, Now);
        PlatformLegalDocument dpa = PlatformLegalDocument.CreateDraft(PlatformLegalDocumentKind.DataProcessing, "d-" + tag, actor, Now);
        PlatformLegalDocument terms = PlatformLegalDocument.CreateDraft(PlatformLegalDocumentKind.Terms, "t-" + tag, actor, Now);
        PlatformLegalDocumentFile file = PlatformLegalDocumentFile.Create(terms.Id, "en", Pdf, "First version", Now);

        await using (PlatformDbContext context = _database.NewContext())
        {
            // Earlier runs may have left published versions; supersede them like the publish use case does.
            foreach (PlatformLegalDocument old in context.LegalDocuments.Where(item => item.Status == PlatformLegalDocumentStatus.Published).ToList())
            {
                old.Supersede(Now);
            }

            context.Tenants.AddRange(accepted, waiting);
            foreach (PlatformLegalDocument document in new[] { privacy, dpa })
            {
                document.Publish(new[] { "en", "fr", "es" }, Array.Empty<PlatformLegalDocumentKind>(), actor, Now);
                context.LegalDocuments.Add(document);
            }

            terms.SetParts(new[] { privacy, dpa }, Now);
            terms.Publish(new[] { "en", "fr", "es" }, Array.Empty<PlatformLegalDocumentKind>(), actor, Now);
            PlatformLegalRepository repository = new(context);
            repository.AddDocument(terms);
            repository.AddFile(file);
            repository.AddAcceptance(PlatformTenantTermsAcceptance.ByOwner(accepted.Id, terms, file, "u-1", "Ada", "owner", "203.0.113.1", "UA", Now));
            repository.AddReminder(PlatformTermsReminder.Automatic(waiting.Id, terms.Id, PlatformTermsReminderStep.Published, "en", Now));
            Assert.True(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
        }

        await using (PlatformDbContext other = _database.NewContext())
        {
            other.TermsReminders.Add(PlatformTermsReminder.Automatic(waiting.Id, terms.Id, PlatformTermsReminderStep.Published, "en", Now));
            Assert.False(await new PlatformUnitOfWork(other).SaveChangesAsync(CancellationToken.None));
        }

        await using (PlatformDbContext other = _database.NewContext())
        {
            PlatformLegalDocument loadedTerms = (await new PlatformLegalRepository(other).GetDocumentAsync(terms.Id, CancellationToken.None))!;
            other.TenantTermsAcceptances.Add(PlatformTenantTermsAcceptance.ByOwner(accepted.Id, loadedTerms, PlatformLegalDocumentFile.Create(terms.Id, "en", Pdf, null, Now), "u-2", "Ada", "owner", null, null, Now));
            Assert.False(await new PlatformUnitOfWork(other).SaveChangesAsync(CancellationToken.None));
        }

        await using (PlatformDbContext other = _database.NewContext())
        {
            PlatformLegalDocument second = PlatformLegalDocument.CreateDraft(PlatformLegalDocumentKind.Privacy, "p2-" + tag, actor, Now);
            second.Publish(new[] { "en", "fr", "es" }, Array.Empty<PlatformLegalDocumentKind>(), actor, Now);
            other.LegalDocuments.Add(second);
            Assert.False(await new PlatformUnitOfWork(other).SaveChangesAsync(CancellationToken.None));
        }

        await using PlatformDbContext check = _database.NewContext();
        PlatformLegalRepository store = new(check);
        PlatformLegalQueries queries = new(check);

        PlatformLegalDocument loaded = (await store.GetPublishedAsync(PlatformLegalDocumentKind.Terms, CancellationToken.None))!;
        Assert.Equal(terms.Id, loaded.Id);
        Assert.Equal(new[] { PlatformLegalDocumentKind.Privacy, PlatformLegalDocumentKind.DataProcessing }, loaded.Parts.Select(part => part.Kind));
        Assert.Equal(terms.Id, (await store.LatestRequiredTermsAsync(CancellationToken.None))!.Id);
        Assert.Equal(file.Sha256, (await store.GetFileAsync(terms.Id, "en", CancellationToken.None))!.Sha256);
        Assert.Equal(Pdf, (await queries.GetFileContentAsync(terms.Id, "en", CancellationToken.None))!.Content);
        Assert.True(await store.HasAcceptedSinceAsync(accepted.Id, loaded.PublishedUtc!.Value, CancellationToken.None));
        Assert.False(await store.HasAcceptedSinceAsync(waiting.Id, loaded.PublishedUtc!.Value, CancellationToken.None));
        Assert.Equal(new[] { PlatformTermsReminderStep.Published }, await store.ListReminderStepsAsync(waiting.Id, terms.Id, CancellationToken.None));

        PagedResult<PlatformLegalRegisterItem> overdue = await queries.RegisterAsync(loaded, new PlatformLegalRegisterFilter(PlatformLegalStatus.Overdue, waiting.TenantCode), PagedRequest.Create(1, 25), Now.AddDays(40), CancellationToken.None);
        PlatformLegalRegisterItem row = Assert.Single(overdue.Items);
        Assert.Equal(waiting.Id, row.PlatformTenantId);
        Assert.Equal(Now.AddDays(30), row.DeadlineUtc);
        Assert.NotNull(row.LastReminderUtc);
        PlatformLegalRegisterItem done = Assert.Single((await queries.RegisterAsync(loaded, new PlatformLegalRegisterFilter(PlatformLegalStatus.Accepted, accepted.TenantCode), PagedRequest.Create(1, 25), Now, CancellationToken.None)).Items);
        Assert.Equal("t-" + tag, done.AcceptedVersion);

        IReadOnlyList<PlatformLegalTenantRow> toRemind = await queries.ListTenantsToRemindAsync(terms.Id, loaded.PublishedUtc, 5000, CancellationToken.None);
        Assert.Contains(toRemind, item => item.PlatformTenantId == waiting.Id && item.Language == "en");
        Assert.DoesNotContain(toRemind, item => item.PlatformTenantId == accepted.Id);
        Assert.DoesNotContain(await queries.ListTenantsToRemindAsync(terms.Id, null, 5000, CancellationToken.None), item => item.PlatformTenantId == waiting.Id);
        Assert.Single(await queries.ListAcceptancesAsync(accepted.Id, CancellationToken.None));
        Assert.Single(await queries.ListRemindersAsync(waiting.Id, CancellationToken.None));
    }

    private static PlatformTenant NewTenant(Guid actor)
    {
        PlatformTenant tenant = PlatformTenant.CreateByStaff(
            "TEN-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            "leg-" + Guid.NewGuid().ToString("N")[..8],
            PlatformCompanyDetails.Create("Orbit Freight LLC", "Orbit " + Guid.NewGuid().ToString("N")[..4], EmailAddress.Create("office@orbit.example"), PhoneNumber.Create("+1 305 555 0199"),
                PlatformPostalAddress.Create("1 Dock Road", null, "Miami", "FL", "33101", "US")),
            PlatformTenantContactDetails.Create(PlatformTenantContactRole.Owner, "Ada Byron", EmailAddress.Create("owner@orbit.example"), null),
            LanguageCode.En,
            "America/New_York",
            actor,
            Now.AddYears(-1)).Tenant;
        tenant.Activate(actor, Now.AddYears(-1));
        return tenant;
    }
}
