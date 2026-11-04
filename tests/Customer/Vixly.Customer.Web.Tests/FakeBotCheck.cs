using Vixly.Customer.Web.Portal;

namespace Vixly.Customer.Web.Tests;

// =========================================================
// FILE: FakeBotCheck.cs
//
// PURPOSE:
// A bot check the test decides (Slice 2).
//
// RESPONSIBILITIES:
// - Pass or fail as the test says; remember the last token
//
// IMPORTANT NOTES:
// - Test-only
// =========================================================
internal sealed class FakeBotCheck : ICustomerBotCheck
{
    public bool Passes { get; set; } = true;

    public string? LastToken { get; private set; }

    public Task<bool> VerifyAsync(string? token, string? clientIp, CancellationToken cancellationToken)
    {
        LastToken = token;
        return Task.FromResult(Passes);
    }
}
