using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Tools.V3Migration;

// =========================================================
// FILE: Migrator.cs
//
// PURPOSE:
// Writes one V3 company's people and open work into its V4 company (owner, 2026-10-23: "people + open work").
//
// RESPONSIBILITIES:
// - Staff (same BCrypt password), the Administrator role where V3 had it
// - Customers with their portal password, active and verified, V3 code kept (without the dash)
// - Customers' home addresses, onto V4 customers that have none, matched by email (owner, 2026-10-26);
//   --addresses-only does just that, for a company migrated before addresses were copied
// - Open shipments with their parcels, received and measured at the counter, V3 numbers kept
// - A shipment V3 already invoiced (issued or collect): booked at V3's amount and invoiced with V3's lines
// - A report of everything copied, skipped and left for the owner
//
// IMPORTANT NOTES:
// - Everything in one transaction: a dry run rolls back, --apply commits; any error rolls back all
// - Safe to run again: a person, customer, shipment or parcel already in V4 (same email or number) is skipped
// - Prices, services, extras, insurance and the company's settings are set up in V4 by the owner first
// - Warehouses are matched by code; the V4 company must have them (with a pickup point at the destination)
// =========================================================
internal sealed class Migrator
{
    private const decimal KilogramsPerPound = 0.45359237m;

    private const decimal CentimetresPerInch = 2.54m;

    private static readonly IReadOnlyDictionary<string, string> Services = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["VXA"] = "air_standard",
        ["VXE"] = "air_express",
        ["VXS"] = "ocean_consolidated",
        ["VXG"] = "ground"
    };

    private readonly IServiceProvider _services;

    private readonly Report _report;

    private readonly bool _pounds;

    private Guid _tenantId;

    public Migrator(IServiceProvider services, Report report, bool pounds)
    {
        _services = services;
        _report = report;
        _pounds = pounds;
    }

    public async Task RunAsync(V3Snapshot v3, Guid tenantId, bool apply, CancellationToken cancellationToken, bool addressesOnly = false)
    {
        _tenantId = tenantId;
        _services.GetRequiredService<IOperationsTenantScope>().Enter(tenantId);
        OperationsDbContext db = _services.GetRequiredService<OperationsDbContext>();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // The connection retries on a passing fault; the whole run is one unit, done again from the start if retried.
        Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            OperationsUser owner = await db.Users.SingleOrDefaultAsync(user => user.IsOwner, cancellationToken)
                ?? throw new InvalidOperationException("The V4 company has no owner yet: register it in Control and let the owner sign in to Operations first.");
            OperationsTenantSettings settings = await db.TenantSettings.SingleOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("The V4 company has no settings yet: the owner opens Settings in Operations and saves them first.");
            Dictionary<string, OperationsWarehouse> warehouses = (await db.Warehouses.ToListAsync(cancellationToken)).ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);

            List<string> missing = v3.Warehouses.Where(item => !warehouses.ContainsKey(item.Code)).Select(item => item.Code).ToList();

            if (missing.Count > 0)
            {
                throw new InvalidOperationException("Create these warehouses in V4 first, with the same codes: " + string.Join(", ", missing) + ".");
            }

            _report.Section("Company", v3.CompanyName + " → V4 company " + tenantId + " (owner " + owner.Email.Value + ")");

            if (addressesOnly)
            {
                await AddressesAsync(db, v3, now, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                await FinishAsync(transaction, apply, cancellationToken);
                return;
            }

            await StaffAsync(db, v3, owner, now, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            Dictionary<Guid, OperationsCustomer> customers = await CustomersAsync(db, v3, now, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            await AddressesAsync(db, v3, now, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            await ShipmentsAsync(db, v3, customers, warehouses, settings, owner, now, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            await FinishAsync(transaction, apply, cancellationToken);
        });
    }

    private async Task FinishAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, bool apply, CancellationToken cancellationToken)
    {
        if (apply)
        {
            await transaction.CommitAsync(cancellationToken);
            _report.Line("Saved: the changes above are in V4.");
        }
        else
        {
            await transaction.RollbackAsync(cancellationToken);
            _report.Line("Dry run: nothing was saved. Run again with --apply to save.");
        }
    }

    // =====================================================
    // HOME ADDRESSES (owner, 2026-10-26)
    // =====================================================
    // Onto V4 customers with no address yet, matched by email; an address V4 cannot read is left for the customer to give at sign-in.
    private async Task AddressesAsync(OperationsDbContext db, V3Snapshot v3, DateTimeOffset now, CancellationToken cancellationToken)
    {
        _report.Section("Addresses", v3.Addresses.Count + " in V3");

        if (v3.AddressNote is { } note)
        {
            _report.Problem("addresses", note + ". None copied: customers give theirs at sign-in.");
            return;
        }

        foreach (V3Customer person in v3.Customers)
        {
            EmailAddress email = EmailAddress.Create(person.Email);

            if (await db.Customers.FirstOrDefaultAsync(customer => customer.Email == email, cancellationToken) is not { } customer)
            {
                continue;
            }

            if (customer.HomeAddress is not null)
            {
                _report.Skip(person.Code, "already has an address in V4");
                continue;
            }

            if (!v3.Addresses.TryGetValue(person.Id, out V3Address? address) || string.IsNullOrWhiteSpace(address.Line1))
            {
                _report.Skip(person.Code, "no address in V3: asked at sign-in");
                continue;
            }

            string? country = Countries.Code(address.Country) ?? Countries.Code(person.Country);

            if (country is null)
            {
                _report.Problem(person.Code, "country \"" + address.Country + "\" cannot be read; address not copied: asked at sign-in");
                continue;
            }

            try
            {
                customer.ChangeHomeAddress(OperationsPostalAddress.Create(address.Line1, address.Line2, address.City, address.Region, address.PostalCode, country), now);
                _report.Copied(person.Code, "address in " + (address.City ?? "?") + ", " + country);
            }
            catch (Vixly.Shared.Kernel.Domain.DomainException exception)
            {
                _report.Problem(person.Code, "address not copied (" + exception.Error.Code + "): asked at sign-in");
            }
        }
    }

    // =====================================================
    // STAFF
    // =====================================================
    private async Task StaffAsync(OperationsDbContext db, V3Snapshot v3, OperationsUser owner, DateTimeOffset now, CancellationToken cancellationToken)
    {
        _report.Section("Staff", v3.Staff.Count + " in V3");
        OperationsRole? administrator = await db.Roles.FirstOrDefaultAsync(role => role.IsAdministrator, cancellationToken);

        foreach (V3Staff person in v3.Staff)
        {
            EmailAddress email = EmailAddress.Create(person.Email);

            if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
            {
                _report.Skip(person.DisplayName, "already in V4 (same email)" + (person.AuthorityLevel == 3 ? "; the V3 owner" : string.Empty));
                continue;
            }

            if (person.AccountStatus != 1 || string.IsNullOrWhiteSpace(person.PasswordHash))
            {
                _report.Skip(person.DisplayName, "no active V3 account; invite them in V4 if needed");
                continue;
            }

            OperationsUser user = OperationsUser.InviteStaff(_tenantId, person.DisplayName, email, LanguageCode.En, now);
            user.AcceptInvitation(person.PasswordHash, now);
            db.Users.Add(user);

            bool admin = person.AuthorityLevel >= 2 || string.Equals(person.Role, "Administrator", StringComparison.OrdinalIgnoreCase);

            if (admin)
            {
                // The Administrator role is made the first time it is needed, as Operations does (OperationsAccess).
                if (administrator is null)
                {
                    administrator = OperationsRole.CreateAdministrator(_tenantId, now);
                    db.Roles.Add(administrator);
                }

                db.RoleGrants.Add(OperationsRoleGrant.Grant(user.TenantId, user.Id, administrator.Id, OperationsRoleScope.Tenant, null, owner.Id, now));
                user.SetAdministrator(true, now);
                _report.Copied(person.DisplayName, "Administrator (sets up the authenticator at first sign-in)");
            }
            else
            {
                _report.Copied(person.DisplayName, person.Role is null ? "no role in V3" : "give the role \"" + person.Role + "\" in V4 Staff");
                _report.Todo("Give " + person.DisplayName + " a role in Staff" + (person.WarehouseCode is null ? string.Empty : " at " + person.WarehouseCode) + ".");
            }
        }
    }

    // =====================================================
    // CUSTOMERS
    // =====================================================
    private async Task<Dictionary<Guid, OperationsCustomer>> CustomersAsync(OperationsDbContext db, V3Snapshot v3, DateTimeOffset now, CancellationToken cancellationToken)
    {
        _report.Section("Customers", v3.Customers.Count + " in V3");
        Dictionary<Guid, OperationsCustomer> map = new();

        foreach (V3Customer person in v3.Customers)
        {
            EmailAddress email = EmailAddress.Create(person.Email);

            if (await db.Customers.FirstOrDefaultAsync(customer => customer.Email == email, cancellationToken) is { } existing)
            {
                map[person.Id] = existing;
                _report.Skip(person.Code, "already in V4 (same email), code " + existing.Code);
                continue;
            }

            (string first, string last) = Split(person.FullName);
            string? phone = Phones.Normalize(person.Phone, person.Country);

            if (phone is null)
            {
                _report.Problem(person.Code, "phone \"" + Phones.Mask(person.Phone) + "\" cannot be read; not copied: add it by hand");
                continue;
            }

            // No V3 password: a random one nobody knows; the customer sets theirs with Forgot password.
            bool samePassword = person.PasswordHash is { Length: > 0 } && person.AccountActive;
            string hash = samePassword ? person.PasswordHash! : _services.GetRequiredService<IOperationsPasswordHasher>().Hash(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
            OperationsCustomer customer = OperationsCustomer.SignUp(_tenantId, first, last, person.Type == 2 ? person.BusinessName : null, email, phone, LanguageCode.En,
                hash, now);
            string code = new(person.Code.Where(char.IsAsciiLetterOrDigit).ToArray());
            customer.ConfirmEmail(code, now);

            if (person.CreditAllowed)
            {
                customer.SetCredit(true, "Copied from V3", now);
            }

            if (person.Status != 1)
            {
                customer.ChangeStatus(OperationsCustomerStatus.Suspended, "Inactive in V3", now);
            }

            db.Customers.Add(customer);
            map[person.Id] = customer;
            _report.Copied(person.Code, "code " + code + (samePassword ? "; same portal password" : "; no portal password: they use Forgot password"));
        }

        return map;
    }

    // =====================================================
    // SHIPMENTS, PARCELS, INVOICES
    // =====================================================
    private async Task ShipmentsAsync(OperationsDbContext db, V3Snapshot v3, Dictionary<Guid, OperationsCustomer> customers, Dictionary<string, OperationsWarehouse> warehouses,
        OperationsTenantSettings settings, OperationsUser owner, DateTimeOffset now, CancellationToken cancellationToken)
    {
        _report.Section("Open shipments", v3.Shipments.Count + " in V3");
        OperationsTracker tracker = _services.GetRequiredService<OperationsTracker>();
        OperationsInvoicing invoicing = _services.GetRequiredService<OperationsInvoicing>();

        foreach (V3Shipment item in v3.Shipments)
        {
            if (await db.Shipments.AnyAsync(shipment => shipment.Number == item.Code, cancellationToken))
            {
                _report.Skip(item.Code, "already in V4");
                continue;
            }

            if (!customers.TryGetValue(item.CustomerId, out OperationsCustomer? customer))
            {
                _report.Problem(item.Code, "its customer was not copied; not copied");
                continue;
            }

            if (item.OriginCode is null || item.DestinationCode is null || !warehouses.TryGetValue(item.OriginCode, out OperationsWarehouse? origin)
                || !warehouses.TryGetValue(item.DestinationCode, out OperationsWarehouse? destination))
            {
                _report.Problem(item.Code, "origin or destination warehouse missing; not copied");
                continue;
            }

            V3Invoice? invoice = v3.Invoices.FirstOrDefault(row => row.ShipmentId == item.Id && row.Status is 1 or 2 or 3);
            bool collect = invoice?.Status == 2;
            OperationsShipmentOptions options = (item.Consolidated ? OperationsShipmentOptions.Consolidate : OperationsShipmentOptions.None)
                | (collect ? OperationsShipmentOptions.FreightCollect : OperationsShipmentOptions.None);
            string? service = item.ServiceCode is { } v3Service && Services.TryGetValue(v3Service, out string? mapped) ? mapped : null;
            string recipientPhone = Phones.Normalize(item.RecipientPhone, destination.Address.CountryCode) ?? customer.Phone;

            OperationsShipment shipment;

            try
            {
                OperationsShipmentInstructions instructions = OperationsShipmentInstructions.Pickup(destination, item.RecipientName ?? customer.FirstName + " " + customer.LastName,
                    recipientPhone, options).ForService(service);
                DateTimeOffset created = new(DateTime.SpecifyKind(item.CreatedUtc, DateTimeKind.Utc));
                shipment = OperationsShipment.Request(_tenantId, customer.Id, origin.Id, instructions, created);
                shipment.OpenedAtCounter();
                shipment.DeclareParcelCount(Math.Max(1, item.ParcelCount), 0, created);
                shipment.Numbered(item.Code);
            }
            catch (DomainException exception)
            {
                _report.Problem(item.Code, "refused by V4 (" + exception.Error.Code + "); not copied");
                continue;
            }

            db.Shipments.Add(shipment);
            List<OperationsParcel> added = new();

            foreach (V3Parcel row in v3.Parcels.Where(parcel => parcel.ShipmentId == item.Id))
            {
                if (row.Weight is not { } weight || row.Length is not { } length || row.Width is not { } width || row.Height is not { } height)
                {
                    _report.Problem(row.Code, "no weight or size in V3; enter it at the counter");
                    continue;
                }

                DateTimeOffset received = new(DateTime.SpecifyKind(row.CreatedUtc, DateTimeKind.Utc));
                OperationsMeasurements measurements = OperationsMeasurements.Create(Kilograms(weight), Centimetres(length), Centimetres(width), Centimetres(height));
                OperationsParcel parcel = OperationsParcel.DropOff(_tenantId, row.Code, shipment, measurements, row.Description ?? "Personal effects", row.DeclaredValue,
                    settings.CurrencyCode, received);
                parcel.TakeStorageTerms(settings.StorageFreeDays, settings.StorageFeePerDay);
                db.Parcels.Add(parcel);
                shipment.TakePackage(received);
                added.Add(parcel);

                // The same history as the counter (C-7): received, then measured.
                string size = string.Join(" x ", new[] { parcel.LengthCm!.Value, parcel.WidthCm!.Value, parcel.HeightCm!.Value }
                    .Select(side => side.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)));
                foreach ((string step, string code, Dictionary<string, string> arguments) in new[]
                {
                    ("received", OperationsTrackingCodes.ReceivedAtOrigin, new Dictionary<string, string>(StringComparer.Ordinal) { ["warehouse"] = origin.Code }),
                    ("checked", OperationsTrackingCodes.Checked, new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["weight"] = parcel.WeightKg!.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                        ["size"] = size
                    })
                })
                {
                    OperationsTrackingDraft draft = new(OperationsTrackingSubject.Parcel, parcel.Id, code, arguments, received, "v3:" + row.Id.ToString("N") + ":" + step,
                        OperationsTrackingActor.Staff, OperationsTrackingSource.Counter)
                    {
                        ActorUserId = owner.Id,
                        LocationCode = origin.Code,
                        Note = "Copied from V3"
                    };

                    if ((await tracker.RecordAsync(draft, cancellationToken)).Error is { } refused)
                    {
                        throw new InvalidOperationException("Tracking of parcel " + row.Code + " was refused: " + refused.Code);
                    }
                }

                _report.Copied(row.Code, "parcel of " + item.Code + ", received " + received.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            }

            _report.Copied(item.Code, origin.Code + " → " + destination.Code + ", " + added.Count + " of " + item.ParcelCount + " parcels" + (service is null ? ", no service" : ", " + service));

            if (invoice is null)
            {
                foreach (V3Invoice draft in v3.Invoices.Where(row => row.ShipmentId == item.Id && row.Status == 0))
                {
                    _report.Todo("V3 draft invoice " + draft.Number + " (" + draft.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ") was never issued: not copied; "
                        + item.Code + " is priced with V4 prices when its last parcel is entered.");
                }

                continue;
            }

            if (added.Count != shipment.ParcelCount || service is null)
            {
                _report.Problem(invoice.Number, "V3 invoiced " + item.Code + " but its parcels or service are incomplete; invoice not copied: check it at the cashier");
                continue;
            }

            // Booked at V3's amount and invoiced with V3's lines: what the customer was told stays what they owe.
            List<OperationsChargeLine> lines = invoice.Lines.Select(line => new OperationsChargeLine(
                line.Description.Contains("fee", StringComparison.OrdinalIgnoreCase) || line.Description.Contains("charge", StringComparison.OrdinalIgnoreCase)
                    ? OperationsChargeLineKind.ServiceFee
                    : OperationsChargeLineKind.Freight,
                line.Description, null, line.Quantity, null, line.Quantity == 0 ? line.Charge : decimal.Round(line.Charge / line.Quantity, 2), line.Charge)).ToList();
            decimal total = lines.Sum(line => line.Amount);
            shipment.Book(total, settings.CurrencyCode, now);
            OperationsRating rating = new(service, Guid.Empty, false, lines, total, false);

            if (await invoicing.IssueForShipmentAsync(shipment, rating, settings.CurrencyCode, null, now, cancellationToken) is not { } issued)
            {
                throw new InvalidOperationException("The invoice of " + item.Code + " was not issued.");
            }

            foreach (OperationsParcel parcel in added)
            {
                OperationsTrackingDraft booked = new(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.Booked, new Dictionary<string, string>(StringComparer.Ordinal), now,
                    "v3:book:" + shipment.Id.ToString("N") + ":" + parcel.Id.ToString("N"), OperationsTrackingActor.Staff, OperationsTrackingSource.Counter)
                {
                    ActorUserId = owner.Id,
                    LocationCode = origin.Code
                };

                if ((await tracker.RecordAsync(booked, cancellationToken)).Error is { } refused)
                {
                    throw new InvalidOperationException("The booked event of " + parcel.Number + " was refused: " + refused.Code);
                }
            }

            _report.Copied(invoice.Number, "→ V4 invoice " + issued.Number + ", " + total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " "
                + settings.CurrencyCode + (collect ? ", to pay at the destination" : string.Empty) + (invoice.Paid > 0 ? "; V3 recorded " + invoice.Paid + " paid: record it at the cashier" : string.Empty));
            _report.Money(invoice.Total, total);
        }
    }

    private decimal Kilograms(decimal weight) => decimal.Round(_pounds ? weight * KilogramsPerPound : weight, 3);

    private decimal Centimetres(decimal side) => decimal.Round(_pounds ? side * CentimetresPerInch : side, 1);

    private static (string First, string Last) Split(string fullName)
    {
        string[] parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => ("Customer", "Customer"),
            1 => (parts[0], parts[0]),
            _ => (string.Join(' ', parts[..^1]), parts[^1])
        };
    }
}
