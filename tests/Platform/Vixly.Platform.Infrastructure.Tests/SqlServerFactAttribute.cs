namespace Vixly.Platform.Infrastructure.Tests;

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
// - The variable holds a server connection string without a
//   database; the tests create and drop their own database
// - Never commit a value: set it in the shell or CI secrets
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
