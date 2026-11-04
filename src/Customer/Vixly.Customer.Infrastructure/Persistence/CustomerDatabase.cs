namespace Vixly.Customer.Infrastructure.Persistence;

// =========================================================
// FILE: CustomerDatabase.cs
//
// PURPOSE:
// Names of the Customer database objects (D-1, ADR-0010).
//
// RESPONSIBILITIES:
// - Name the schema, history table and connection string
//
// IMPORTANT NOTES:
// - The customer schema holds only the Data Protection key ring: business data is read through Operations.Api
// =========================================================
public static class CustomerDatabase
{
    public const string Schema = "customer";

    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    public const string ConnectionStringName = "Customer";
}
