namespace Vixly.Operations.Infrastructure.Persistence;

// =========================================================
// FILE: OperationsDatabase.cs
//
// PURPOSE:
// Names of the Operations database objects (D-1).
//
// RESPONSIBILITIES:
// - Name the schema, history table and connection string
//
// IMPORTANT NOTES:
// - Schema ops; tables unprefixed (Global Standard 4.1)
// =========================================================
public static class OperationsDatabase
{
    public const string Schema = "ops";

    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    public const string ConnectionStringName = "Operations";
}
