namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: SqlServerFactAttribute.cs
//
// PURPOSE:
// A fact that runs only when a test SQL Server is configured.
//
// RESPONSIBILITIES:
// - Skip with a clear message when VIXLY_TEST_SQL_SERVER is unset
//
// IMPORTANT NOTES:
// - Never commit a value
// =========================================================
public sealed class SqlServerFactAttribute : FactAttribute
{
    public const string Variable = "VIXLY_TEST_SQL_SERVER";

    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable)))
        {
            Skip = $"Set {Variable} to run database tests.";
        }
    }
}
