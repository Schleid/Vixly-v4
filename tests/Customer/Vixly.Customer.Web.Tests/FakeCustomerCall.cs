using System.Text.Json;

namespace Vixly.Customer.Web.Tests;

// =========================================================
// FILE: FakeCustomerCall.cs
//
// PURPOSE:
// One call of the customer portal to the fake Operations.Api (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the method, path, session and body; read a body field
//
// IMPORTANT NOTES:
// - Test-only
// =========================================================
internal sealed record FakeCustomerCall(string Method, string Path, string? Session, JsonElement? Body)
{
    public string Field(string name) => Body?.GetProperty(name).GetString() ?? string.Empty;
}
