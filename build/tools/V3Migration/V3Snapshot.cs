using Microsoft.Data.SqlClient;

namespace Vixly.Tools.V3Migration;

// =========================================================
// FILE: V3Snapshot.cs
//
// PURPOSE:
// Reads one V3 company's people and open work, read-only (Global Standard M-2).
//
// RESPONSIBILITIES:
// - Staff with their account, role and warehouse
// - Customers with their portal account, and their address (owner, 2026-10-26)
// - Warehouses, open shipments, their parcels and invoices
//
// IMPORTANT NOTES:
// - Only SELECT statements; the connection is opened with ApplicationIntent=ReadOnly
// - V3 enums are read as numbers (V3 Operations.Domain/Enums): shipment 1 Draft, 2 Created;
//   invoice 0 Draft, 1 Issued, 2 Collect, 3 Paid, 4 Cancelled
// - The address columns of dbo.Customers are found by name (V3's schema drifted from its model);
//   when they cannot be found, no address is read and the report says why
// =========================================================
internal sealed record V3Staff(Guid Id, string Email, string DisplayName, string? Phone, int AuthorityLevel, string? Role, string? WarehouseCode, string? PasswordHash, int AccountStatus);

internal sealed record V3Customer(Guid Id, string Code, int Type, string? BusinessName, string FullName, string Email, string? Phone, string? Country, int Status, bool CreditAllowed,
    string? PasswordHash, bool AccountActive);

internal sealed record V3Address(string? Line1, string? Line2, string? City, string? Region, string? PostalCode, string? Country);

internal sealed record V3Warehouse(Guid Id, string Code, string Name, string Country, bool IsDispatchHub);

internal sealed record V3Parcel(Guid Id, Guid ShipmentId, string Code, string? Description, decimal? Length, decimal? Width, decimal? Height, decimal? Weight, decimal? DeclaredValue,
    DateTime CreatedUtc);

internal sealed record V3InvoiceLine(string Description, decimal Quantity, decimal Charge, int Order);

internal sealed record V3Invoice(Guid Id, Guid? ShipmentId, string Number, int Status, decimal Total, decimal Paid, IReadOnlyList<V3InvoiceLine> Lines);

internal sealed record V3Shipment(Guid Id, Guid CustomerId, string Code, string? OriginCode, string? DestinationCode, string? ServiceCode, int ParcelCount, string? RecipientName,
    string? RecipientPhone, bool Consolidated, bool Insured, int Status, DateTime CreatedUtc);

internal sealed class V3Snapshot
{
    public required Guid TenantId { get; init; }

    public required string CompanyName { get; init; }

    public required IReadOnlyList<V3Staff> Staff { get; init; }

    public required IReadOnlyList<V3Customer> Customers { get; init; }

    public required IReadOnlyList<V3Warehouse> Warehouses { get; init; }

    public required IReadOnlyList<V3Shipment> Shipments { get; init; }

    public required IReadOnlyList<V3Parcel> Parcels { get; init; }

    public required IReadOnlyList<V3Invoice> Invoices { get; init; }

    public required IReadOnlyDictionary<Guid, V3Address> Addresses { get; init; }

    // Why no address was read, when none could be.
    public string? AddressNote { get; init; }

    public static async Task<V3Snapshot> ReadAsync(string connectionString, Guid? tenantId, CancellationToken cancellationToken)
    {
        SqlConnectionStringBuilder builder = new(connectionString) { ApplicationIntent = ApplicationIntent.ReadOnly };
        await using SqlConnection connection = new(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        List<(Guid Id, string Name)> companies = await ListAsync(connection, "SELECT p.TenantId, p.CompanyName FROM dbo.TenantCompanyProfiles p", null,
            reader => (reader.GetGuid(0), reader.GetString(1)), cancellationToken);
        (Guid Id, string Name) company = tenantId is { } chosen
            ? companies.FirstOrDefault(item => item.Id == chosen)
            : companies.Count == 1 ? companies[0] : throw new InvalidOperationException("V3 has " + companies.Count + " companies: give --v3-tenant.");

        if (company.Id == Guid.Empty)
        {
            throw new InvalidOperationException("V3 has no company " + tenantId + ".");
        }

        Guid tenant = company.Id;

        List<V3Staff> staff = await ListAsync(connection, """
            SELECT u.Id, u.Email, u.DisplayName, u.PhoneNumber, u.AuthorityLevel, r.Name, w.Code, a.PasswordHash, ISNULL(a.Status, 0)
            FROM dbo.TenantUsers u
            LEFT JOIN dbo.TenantStaffRoles r ON r.Id = u.RoleId
            LEFT JOIN dbo.Warehouses w ON w.Id = u.WarehouseId
            LEFT JOIN dbo.TenantUserAccounts a ON a.TenantUserId = u.Id
            WHERE u.TenantId = @tenant
            """, tenant, reader => new V3Staff(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), Text(reader, 3), Convert.ToInt32(reader.GetValue(4)), Text(reader, 5),
            Text(reader, 6), Text(reader, 7), Convert.ToInt32(reader.GetValue(8))), cancellationToken);

        List<V3Customer> customers = await ListAsync(connection, """
            SELECT c.Id, c.CustomerCode, c.Type, c.BusinessName, c.FullName, c.Email, c.PhoneNumber, c.Country, c.Status, c.IsCreditAllowed, a.PasswordHash, ISNULL(a.IsActive, 0)
            FROM dbo.Customers c
            LEFT JOIN dbo.CustomerAccounts a ON a.CustomerId = c.Id
            WHERE c.TenantId = @tenant
            """, tenant, reader => new V3Customer(reader.GetGuid(0), reader.GetString(1), Convert.ToInt32(reader.GetValue(2)), Text(reader, 3), reader.GetString(4), reader.GetString(5),
            Text(reader, 6), Text(reader, 7), Convert.ToInt32(reader.GetValue(8)), reader.GetBoolean(9), Text(reader, 10), reader.GetBoolean(11)), cancellationToken);

        List<V3Warehouse> warehouses = await ListAsync(connection,
            "SELECT Id, Code, Name, Country, IsDispatchHub FROM dbo.Warehouses WHERE TenantId = @tenant", tenant,
            reader => new V3Warehouse(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetBoolean(4)), cancellationToken);

        // Open work only: draft and created shipments (V3 ShipmentStatus 1 and 2).
        List<V3Shipment> shipments = await ListAsync(connection, """
            SELECT s.Id, s.CustomerId, s.ShipmentCode, ow.Code, dw.Code, ps.Code, s.ParcelCount, s.RecipientName, s.RecipientPhoneNumber, s.IsConsolidated, s.IsInsured, s.Status, s.CreatedOnUtc
            FROM dbo.Shipments s
            LEFT JOIN dbo.Warehouses ow ON ow.Id = s.OriginWarehouseId
            LEFT JOIN dbo.Warehouses dw ON dw.Id = s.DestinationWarehouseId
            LEFT JOIN dbo.TenantShippingServiceSetups ts ON ts.Id = s.ServiceTypeId
            LEFT JOIN dbo.PlatformShippingServices ps ON ps.Id = ts.PlatformShippingServiceId
            WHERE s.TenantId = @tenant AND s.Status IN (1, 2)
            """, tenant, reader => new V3Shipment(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), Text(reader, 3), Text(reader, 4), Text(reader, 5),
            Convert.ToInt32(reader.GetValue(6)), Text(reader, 7), Text(reader, 8), reader.GetBoolean(9), reader.GetBoolean(10), Convert.ToInt32(reader.GetValue(11)),
            reader.GetDateTime(12)), cancellationToken);

        List<V3Parcel> parcels = await ListAsync(connection, """
            SELECT p.Id, p.ShipmentId, p.ParcelCode, p.Description, p.Length, p.Width, p.Height, p.Weight, p.DeclaredValue, p.CreatedOnUtc
            FROM dbo.Parcels p JOIN dbo.Shipments s ON s.Id = p.ShipmentId
            WHERE s.TenantId = @tenant
            """, tenant, reader => new V3Parcel(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), Text(reader, 3), Number(reader, 4), Number(reader, 5), Number(reader, 6),
            Number(reader, 7), Number(reader, 8), reader.GetDateTime(9)), cancellationToken);

        List<(Guid Invoice, V3InvoiceLine Line)> lines = await ListAsync(connection, """
            SELECT l.InvoiceId, l.Description, l.Quantity, l.Charge, l.DisplayOrder
            FROM dbo.CustomerInvoiceLines l JOIN dbo.CustomerInvoices i ON i.Id = l.InvoiceId
            WHERE i.TenantId = @tenant
            """, tenant, reader => (reader.GetGuid(0), new V3InvoiceLine(reader.GetString(1), Convert.ToDecimal(reader.GetValue(2)), Convert.ToDecimal(reader.GetValue(3)),
            Convert.ToInt32(reader.GetValue(4)))), cancellationToken);

        List<V3Invoice> invoices = await ListAsync(connection,
            "SELECT Id, ShipmentId, InvoiceNumber, Status, TotalAmount, AmountPaid FROM dbo.CustomerInvoices WHERE TenantId = @tenant", tenant,
            reader =>
            {
                Guid id = reader.GetGuid(0);
                return new V3Invoice(id, reader.IsDBNull(1) ? null : reader.GetGuid(1), reader.GetString(2), Convert.ToInt32(reader.GetValue(3)), Convert.ToDecimal(reader.GetValue(4)),
                    Convert.ToDecimal(reader.GetValue(5)), lines.Where(line => line.Invoice == id).Select(line => line.Line).OrderBy(line => line.Order).ToList());
            }, cancellationToken);

        (Dictionary<Guid, V3Address> addresses, string? addressNote) = await AddressesAsync(connection, tenant, cancellationToken);

        return new V3Snapshot
        {
            TenantId = tenant,
            CompanyName = company.Name,
            Staff = staff,
            Customers = customers,
            Warehouses = warehouses,
            Shipments = shipments,
            Parcels = parcels,
            Invoices = invoices,
            Addresses = addresses,
            AddressNote = addressNote
        };
    }

    // The customers' addresses, from whichever of the usual column names V3 has.
    private static async Task<(Dictionary<Guid, V3Address>, string?)> AddressesAsync(SqlConnection connection, Guid tenant, CancellationToken cancellationToken)
    {
        HashSet<string> columns = (await ListAsync(connection,
            "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Customers'", null, reader => reader.GetString(0), cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string? Pick(params string[] names) => names.FirstOrDefault(columns.Contains);

        string? line1 = Pick("AddressLine1", "Address_Line1", "Address_AddressLine1", "Line1", "Address1", "StreetAddress", "Address");
        string? line2 = Pick("AddressLine2", "Address_Line2", "Address_AddressLine2", "Line2", "Address2");
        string? city = Pick("City", "Address_City");
        string? region = Pick("State", "Address_State", "Region", "Address_Region", "StateOrProvince");
        string? postal = Pick("PostalCode", "Address_PostalCode", "ZipCode", "Zip", "Address_ZipCode");
        string? country = Pick("Country", "Address_Country", "CountryCode", "Address_CountryCode");

        if (line1 is null || city is null)
        {
            return (new Dictionary<Guid, V3Address>(), "V3 Customers has no street or city column; columns seen: " + string.Join(", ", columns.Order(StringComparer.OrdinalIgnoreCase)));
        }

        static string Column(string? name) => name is null ? "NULL" : "[" + name + "]";
        List<(Guid Id, V3Address Address)> rows = await ListAsync(connection,
            "SELECT Id, " + string.Join(", ", new[] { line1, line2, city, region, postal, country }.Select(Column)) + " FROM dbo.Customers WHERE TenantId = @tenant", tenant,
            reader => (reader.GetGuid(0), new V3Address(Text(reader, 1), Text(reader, 2), Text(reader, 3), Text(reader, 4), Text(reader, 5), Text(reader, 6))), cancellationToken);

        return (rows.ToDictionary(row => row.Id, row => row.Address), null);
    }

    private static string? Text(SqlDataReader reader, int index) => reader.IsDBNull(index) ? null : Convert.ToString(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture);

    private static decimal? Number(SqlDataReader reader, int index) => reader.IsDBNull(index) ? null : Convert.ToDecimal(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture);

    private static async Task<List<T>> ListAsync<T>(SqlConnection connection, string sql, Guid? tenant, Func<SqlDataReader, T> map, CancellationToken cancellationToken)
    {
        await using SqlCommand command = new(sql, connection);

        if (tenant is { } id)
        {
            command.Parameters.AddWithValue("@tenant", id);
        }

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        List<T> rows = new();

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(map(reader));
        }

        return rows;
    }
}
