namespace Vixly.Platform.Infrastructure.Persistence;

// =========================================================
// FILE: PlatformDatabase.cs
//
// PURPOSE:
// The names that place Platform data in a database (Global
// Standard D-1).
//
// RESPONSIBILITIES:
// - Name the Platform schema
// - Name the migrations history table, kept in that schema
// - Name the connection string the Platform hosts read
//
// IMPORTANT NOTES:
// - The schema is the only thing that separates Platform data
//   from other planes when they share a database, so it is
//   never changed and never "dbo"
// - The same database may also hold the ops and customer
//   schemas, or Platform may have a database of its own; code
//   must not depend on which
// =========================================================
public static class PlatformDatabase
{
    public const string Schema = "platform";

    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    public const string ConnectionStringName = "Platform";
}
