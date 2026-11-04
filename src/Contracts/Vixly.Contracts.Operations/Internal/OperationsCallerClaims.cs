namespace Vixly.Contracts.Operations.Internal;

// =========================================================
// FILE: OperationsCallerClaims.cs
//
// PURPOSE:
// Names shared by the callers of Operations.Api and its token check (Stage 3, Slice 0).
//
// RESPONSIBILITIES:
// - Name the audience, the header and the claims of a caller token
//
// IMPORTANT NOTES:
// - ES256, at most 2 minutes, single use, as ADR-0014
// - The customer id is trusted only from a valid token (S-2, S-3)
// =========================================================
public static class OperationsCallerClaims
{
    public const string Audience = "vixly-operations-internal";

    public const string CustomerCaller = "customer";

    public const string CustomerScope = "customer";

    public const string TenantId = "tid";

    public const string CustomerId = "cid";

    // Customer.Web passes the customer's session and the visitor's address and browser (Slice 2).
    public const string SessionHeader = "X-Customer-Session";

    public const string ClientIpHeader = "X-Customer-Ip";

    public const string UserAgentHeader = "X-Customer-Agent";
}
