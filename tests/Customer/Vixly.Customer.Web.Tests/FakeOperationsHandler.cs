using System.Buffers.Text;
using System.Net.Http.Json;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using Vixly.Contracts.Operations.Customers;
using Vixly.Contracts.Operations.Internal;

namespace Vixly.Customer.Web.Tests;

// =========================================================
// FILE: FakeOperationsHandler.cs
//
// PURPOSE:
// Stands in for Operations.Api behind the Customer.Web HTTP client.
//
// RESPONSIBILITIES:
// - Answer the tenant lookup
// - Check every token Customer.Web signs
//
// IMPORTANT NOTES:
// - A token that fails the check gets 401, as the real API would answer
// =========================================================
internal sealed class FakeOperationsHandler : HttpMessageHandler
{
    private readonly ECDsa _key;

    public FakeOperationsHandler(ECDsa key)
    {
        _key = key;
    }

    public Dictionary<string, OperationsTenantHostDto> Tenants { get; } = new(StringComparer.Ordinal);

    public bool Down { get; set; }

    public int Calls { get; private set; }

    public List<JsonElement> Claims { get; } = new();

    // Slice 2: every portal call, and the answers the test chooses (null: 404).
    public List<FakeCustomerCall> Requests { get; } = new();

    public Func<FakeCustomerCall, (HttpStatusCode Status, object? Body)>? Customers { get; set; }

    public CustomerPortalBrandDto Brand { get; set; } = new("Northwind Couriers", null, null, "#123456", "#0F766E");

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;

        if (Down)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }

        string token = request.Headers.Authorization?.Parameter ?? string.Empty;
        string[] parts = token.Split('.');

        if (request.Headers.Authorization?.Scheme != "Bearer" || parts.Length != 3
            || !_key.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), Base64Url.DecodeFromChars(parts[2]), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }

        Claims.Add(JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1])).RootElement.Clone());

        if (request.RequestUri!.AbsolutePath.StartsWith("/internal/v1/customers/", StringComparison.Ordinal))
        {
            return AnswerCustomerAsync(request, cancellationToken);
        }
        string slug = request.RequestUri!.AbsolutePath.Split('/')[^1];
        OperationsTenantHostDto answer = Tenants.TryGetValue(slug, out OperationsTenantHostDto? found)
            ? found
            : new OperationsTenantHostDto(false, null, "not_found", "none", null, null);

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(answer) });
    }

    private async Task<HttpResponseMessage> AnswerCustomerAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string path = request.RequestUri!.AbsolutePath["/internal/v1/customers/".Length..];
        JsonElement? body = request.Content is null ? null : JsonDocument.Parse(await request.Content.ReadAsStringAsync(cancellationToken)).RootElement.Clone();
        FakeCustomerCall call = new(request.Method.Method, path,
            request.Headers.TryGetValues(OperationsCallerClaims.SessionHeader, out IEnumerable<string>? sessions) ? sessions.Single() : null, body);
        Requests.Add(call);

        if (path == "brand" && request.Method == HttpMethod.Get)
        {
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Brand) };
        }

        (HttpStatusCode status, object? answer) = Customers?.Invoke(call) ?? (HttpStatusCode.NotFound, null);
        HttpResponseMessage response = new(status);

        if (answer is not null)
        {
            response.Content = JsonContent.Create(answer, answer.GetType());
        }

        return response;
    }
}
