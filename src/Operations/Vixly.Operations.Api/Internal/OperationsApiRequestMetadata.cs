using Vixly.Contracts.Operations.Internal;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Api.Internal;

// =========================================================
// FILE: OperationsApiRequestMetadata.cs
//
// PURPOSE:
// The visitor's address and browser, as Customer.Web passed them (Slice 2).
//
// RESPONSIBILITIES:
// - Read the X-Customer-Ip and X-Customer-Agent headers of a checked caller
//
// IMPORTANT NOTES:
// - Only internal requests reach here, and only after the caller token is checked
// =========================================================
public sealed class OperationsApiRequestMetadata : IOperationsRequestMetadata
{
    private readonly IHttpContextAccessor _http;

    public OperationsApiRequestMetadata(IHttpContextAccessor http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public string? ClientIp => Header(OperationsCallerClaims.ClientIpHeader, 45);

    public string? UserAgent => Header(OperationsCallerClaims.UserAgentHeader, 512);

    public string? CorrelationId => _http.HttpContext?.TraceIdentifier;

    private string? Header(string name, int maxLength)
    {
        string? value = _http.HttpContext?.Request.Headers[name].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Length > maxLength ? value[..maxLength] : value;
    }
}
