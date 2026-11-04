namespace Vixly.Operations.Web.Security;

// =========================================================
// FILE: OperationsClaimTypes.cs
//
// PURPOSE:
// Claim names of a signed-in staff member.
//
// RESPONSIBILITIES:
// - Name every claim in one place
//
// IMPORTANT NOTES:
// - Built from the server-side session on every request
// =========================================================
public static class OperationsClaimTypes
{
    public const string UserId = "ops:uid";

    public const string SessionId = "ops:sid";

    public const string DisplayName = "ops:name";

    public const string Language = "ops:lang";

    public const string Owner = "ops:owner";

    public const string Administrator = "ops:admin";

    public const string Setup = "ops:setup";
}
