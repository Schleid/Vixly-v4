using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformDemoTenants.cs
//
// PURPOSE:
// Development-only mock data: tenants and registrations that look
// like a running business.
//
// RESPONSIBILITIES:
// - Create tenants the way production does: from a registration
//   whose email was verified (owner correction, 2026-10-14)
// - Move them through the lifecycle with history, contacts, tags,
//   account managers and notes
// - Leave some registrations pending, cancelled or expired
//
// IMPORTANT NOTES:
// - Every record is tagged or named "demo"; emails use .test
// - Fictional companies only; no real client is named
// - Writes through the Application ports and the domain, so every
//   invariant holds; no audit entries or notices are produced
// - Refuses to run twice (looks for the "demo" tag)
// =========================================================
public sealed class PlatformDemoTenants
{
    public const string Tag = "demo";

    private static readonly string[] Companies =
    {
        "Northwind Freight|CA|fr|America/Toronto|+1 514 555 0100|Montreal|QC|ocean,enterprise",
        "Maple Route Couriers|CA|en|America/Vancouver|+1 604 555 0101|Vancouver|BC|courier,sme",
        "Bluewater Shipping|US|en|America/New_York|+1 212 555 0102|New York|NY|ocean,enterprise",
        "Lone Star Cargo|US|en|America/Chicago|+1 713 555 0103|Houston|TX|air,sme",
        "Transports Belle Rive|FR|fr|Europe/Paris|+33 1 55 50 01 04|Lyon|null|road,sme",
        "Atlantique Logistique|FR|fr|Europe/Paris|+33 2 55 50 01 05|Nantes|null|ocean,pilot",
        "Envíos del Sol|ES|es|Europe/Madrid|+34 910 555 106|Madrid|null|courier,sme",
        "Carga Andina|CO|es|America/Bogota|+57 601 555 0107|Bogotá|null|air,pilot",
        "Puerto Azul Logística|MX|es|America/Mexico_City|+52 55 5550 0108|Veracruz|VER|ocean,enterprise",
        "Pampa Express|AR|es|America/Argentina/Buenos_Aires|+54 11 5550 0109|Buenos Aires|null|road,sme",
        "Kingston Port Services|JM|en|America/Jamaica|+1 876 555 0110|Kingston|null|ocean,sme",
        "Caraïbes Fret|HT|fr|America/Port-au-Prince|+509 2255 0111|Port-au-Prince|null|air,courier",
        "Dakar Transit|SN|fr|Africa/Dakar|+221 33 555 0112|Dakar|null|ocean,pilot",
        "Abidjan Cargo Express|CI|fr|Africa/Abidjan|+225 27 2255 0113|Abidjan|null|air,sme",
        "Atlas Messagerie|MA|fr|Africa/Casablanca|+212 522 555 114|Casablanca|null|courier,sme",
        "Lisboa Marítima|PT|es|Europe/Lisbon|+351 21 555 0115|Lisbon|null|ocean,sme",
        "Flandres Logistics|BE|fr|Europe/Brussels|+32 2 555 01 16|Antwerp|null|road,enterprise",
        "Thames Forwarding|GB|en|Europe/London|+44 20 5550 0117|London|null|air,enterprise",
        "Santo Domingo Envíos|DO|es|America/Santo_Domingo|+1 809 555 0118|Santo Domingo|null|courier,sme",
        "Rhein Spedition|DE|en|Europe/Berlin|+49 30 5550 0119|Hamburg|null|ocean,pilot",
        "Andes Courier|PE|es|America/Lima|+51 1 555 0120|Lima|null|courier,sme",
        "Douala Shipping|CM|fr|Africa/Douala|+237 233 555 121|Douala|null|ocean,sme"
    };

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformPortalSlugRepository _slugs;

    private readonly IPlatformTenantRegistrationRepository _registrations;

    private readonly IPlatformTenantQueries _queries;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformTenantPolicy _policy;

    private readonly PlatformTenantAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public PlatformDemoTenants(
        IPlatformTenantRepository tenants,
        IPlatformPortalSlugRepository slugs,
        IPlatformTenantRegistrationRepository registrations,
        IPlatformTenantQueries queries,
        IPlatformSecretGenerator secrets,
        IPlatformTenantPolicy policy,
        PlatformTenantAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _slugs = slugs ?? throw new ArgumentNullException(nameof(slugs));
        _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<string> SeedAsync(CancellationToken cancellationToken)
    {
        PagedResult<PlatformTenantListItem> existing = await _queries.ListAsync(
            new PlatformTenantListFilter(null, null, Tag, null),
            PagedRequest.First,
            cancellationToken);

        if (existing.TotalCount > 0)
        {
            return "Demo tenants already exist; nothing was added.";
        }

        IReadOnlyList<PlatformStaffOptionItem> staff = await _queries.ListActiveStaffAsync(cancellationToken);

        if (staff.Count == 0)
        {
            return "Create and sign in the first SuperUser before seeding demo tenants.";
        }

        DateTimeOffset now = _administration.UtcNow;
        int tenants = 0;

        for (int index = 0; index < Companies.Length; index++)
        {
            await SeedTenantAsync(Companies[index].Split('|'), index, staff, now, cancellationToken);
            tenants++;
        }

        int registrations = await SeedOpenRegistrationsAsync(staff[0].PlatformUserId, now, cancellationToken);

        return $"Added {tenants} demo tenants and {registrations} open or closed registrations (tag \"{Tag}\").";
    }

    private async Task SeedTenantAsync(
        string[] row,
        int index,
        IReadOnlyList<PlatformStaffOptionItem> staff,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        string name = row[0];
        string slug = Slug(name);
        DateTimeOffset started = now.AddDays(-(400 - (index * 17))).AddHours(-index);
        Guid actor = staff[index % staff.Count].PlatformUserId;

        PlatformTenantRegistration registration = Start(row, slug, started, Owner(index));
        registration.RecordVerificationSent(started);
        PlatformPortalSlug claim = PlatformPortalSlug.ClaimForRegistration(slug, registration.Id, registration.VerificationDeadlineUtc, started);

        DateTimeOffset verified = started.AddHours(2);
        PlatformTenantCreation creation = PlatformTenant.CreateFromRegistration(
            await _administration.NewTenantCodeAsync(cancellationToken) ?? "TEN-DEMO" + index.ToString("D2", System.Globalization.CultureInfo.InvariantCulture),
            registration,
            verified);
        PlatformTenant tenant = creation.Tenant;
        registration.MarkEmailVerified(tenant.Id, verified);
        claim.MakePermanent(registration.Id, tenant.Id, verified);

        List<PlatformTenantLifecycleEvent> events = new(creation.Events);
        string[] tags = row[7].Split(',').Append(Tag).ToArray();
        tenant.SetTags(tags, verified);

        if (index % 3 == 0)
        {
            tenant.ReplaceContacts(
                new[]
                {
                    Contact(PlatformTenantContactRole.Owner, registration.OwnerName, registration.OwnerEmail.Value),
                    Contact(PlatformTenantContactRole.Billing, "Accounts Payable", $"billing@{slug}.test"),
                    Contact(PlatformTenantContactRole.Technical, "IT Desk", $"it@{slug}.test")
                },
                verified);
        }

        if (index % 2 == 0)
        {
            tenant.AssignAccountManager(actor, verified);
        }

        // Status mix: 3 drafts, 2 suspended, 2 deactivated, 1 banned, the rest active.
        if (index is not (5 or 13 or 21))
        {
            events.Add(tenant.Activate(actor, verified.AddDays(3)));
        }

        switch (index)
        {
            case 4:
                events.Add(tenant.Suspend(PlatformSuspensionCategory.Compliance, "Import licence expired; renewal requested.", actor, verified.AddDays(90)));
                break;
            case 9:
                events.Add(tenant.Suspend(PlatformSuspensionCategory.Security, "Shared admin password reported; waiting for reset.", actor, verified.AddDays(60)));
                break;
            case 7:
                events.Add(tenant.Suspend(PlatformSuspensionCategory.Administrative, "Duplicate account found.", actor, verified.AddDays(20)));
                events.Add(tenant.Reactivate("Accounts merged.", actor, verified.AddDays(25)));
                break;
            case 11:
                events.Add(tenant.Deactivate("Business closed at year end.", actor, verified.AddDays(150)));
                break;
            case 17:
                events.Add(tenant.Deactivate("Moved to another provider.", actor, verified.AddDays(80)));
                break;
            case 15:
                events.Add(tenant.Suspend(PlatformSuspensionCategory.FraudInvestigation, "Chargebacks on several card payments.", actor, verified.AddDays(40)));
                events.Add(tenant.Ban("Fraud confirmed by the payment provider.", actor, verified.AddDays(55)));
                break;
        }

        _registrations.Add(registration);
        _slugs.Add(claim);
        _tenants.Add(tenant);
        _tenants.AddEvents(events);

        if (index % 4 == 1)
        {
            _tenants.AddNote(PlatformTenantNote.Write(tenant.Id, "Kick-off call done. Wants weekly pickup reports.", actor, verified.AddDays(4)));
            _tenants.AddNote(PlatformTenantNote.Write(tenant.Id, "Asked about the White Label bundle for next year.", actor, verified.AddDays(30)));
        }

        await SaveAsync(name, cancellationToken);
    }

    private async Task<int> SeedOpenRegistrationsAsync(Guid actor, DateTimeOffset now, CancellationToken cancellationToken)
    {
        string[][] rows =
        {
            "Harbor Lane Movers|US|en|America/Los_Angeles|+1 310 555 0201|Los Angeles|CA|demo".Split('|'),
            "Express Québec|CA|fr|America/Toronto|+1 418 555 0202|Québec|QC|demo".Split('|'),
            "Rápido Caribe|DO|es|America/Santo_Domingo|+1 829 555 0203|Santiago|null|demo".Split('|'),
            "Sahel Fret|SN|fr|Africa/Dakar|+221 33 555 0204|Thiès|null|demo".Split('|'),
            "Costa Verde Cargo|CL|es|America/Santiago|+56 2 2555 0205|Valparaíso|null|demo".Split('|')
        };

        for (int index = 0; index < rows.Length; index++)
        {
            string slug = Slug(rows[index][0]);
            DateTimeOffset started = index == 4 ? now.AddDays(-3) : now.AddHours(-(index + 1) * 3);
            PlatformTenantRegistration registration = Start(rows[index], slug, started, Owner(30 + index));
            registration.RecordVerificationSent(started);
            PlatformPortalSlug claim = PlatformPortalSlug.ClaimForRegistration(slug, registration.Id, registration.VerificationDeadlineUtc, started);

            _registrations.Add(registration);

            if (index == 3)
            {
                registration.Cancel(actor, "Duplicate of an existing tenant.", started.AddHours(1));
            }
            else
            {
                _slugs.Add(claim);
            }

            if (index == 4)
            {
                registration.ExpireIfDue(now);
            }

            await SaveAsync(rows[index][0], cancellationToken);
        }

        return rows.Length;
    }

    private PlatformTenantRegistration Start(string[] row, string slug, DateTimeOffset started, (string Name, string Email) owner)
    {
        PlatformCompanyDetails company = PlatformCompanyDetails.Create(
            row[0] + (row[1] is "FR" or "BE" or "SN" or "CI" or "MA" or "HT" or "CM" ? " SARL" : row[2] == "es" ? " S.A." : " Inc."),
            row[0],
            EmailAddress.Create($"office@{slug}.test"),
            PhoneNumber.Create(row[4]),
            PlatformPostalAddress.Create("1 Demo Street", null, row[5], row[6] == "null" ? null : row[6], null, row[1]));

        return PlatformTenantRegistration.Start(
            company,
            owner.Name,
            EmailAddress.Create(owner.Email.Replace("@demo.test", $"@{slug}.test", StringComparison.Ordinal)),
            PhoneNumber.Create(row[4]),
            LanguageCode.Create(row[2]),
            row[3],
            slug,
            _secrets.NewToken().Hash,
            true,
            _policy.TermsVersion,
            _policy.PrivacyVersion,
            "198.51.100." + (started.Second + 10).ToString(System.Globalization.CultureInfo.InvariantCulture),
            started);
    }

    private async Task SaveAsync(string name, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            throw new InvalidOperationException($"Could not save demo data for {name} (slug or code already used).");
        }
    }

    private static PlatformTenantContactDetails Contact(PlatformTenantContactRole role, string name, string email)
    {
        return PlatformTenantContactDetails.Create(role, name, EmailAddress.Create(email), null);
    }

    private static (string Name, string Email) Owner(int index)
    {
        string[] first = { "Grace", "Amadou", "Lucía", "Marc", "Aisha", "Diego", "Claire", "Kwame", "Sofía", "Liam", "Nadia", "Tomás" };
        string[] last = { "Martin", "Diallo", "García", "Tremblay", "Benali", "Rojas", "Dubois", "Mensah", "Pérez", "Walsh", "Haddad", "Silva" };
        string person = $"{first[index % first.Length]} {last[(index * 5) % last.Length]}";

        return (person, $"{first[index % first.Length].ToLowerInvariant()}@demo.test");
    }

    private static string Slug(string name)
    {
        string plain = new string(name.Normalize(System.Text.NormalizationForm.FormD)
            .Where(character => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray()).ToLowerInvariant();
        string slug = string.Join('-', plain.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => new string(word.Where(char.IsAsciiLetterOrDigit).ToArray()))
            .Where(word => word.Length > 0));

        return slug.Length > 40 ? slug[..40].TrimEnd('-') : slug;
    }
}
