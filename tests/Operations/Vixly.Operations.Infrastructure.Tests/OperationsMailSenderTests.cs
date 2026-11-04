using System.Globalization;
using System.Net.Mail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Vixly.Operations.Infrastructure.Mail;
using Vixly.Operations.Resources;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsMailSenderTests.cs
//
// PURPOSE:
// Proves company emails are designed under the company's name and never show Vixly (owner, 2026-10-25).
//
// RESPONSIBILITIES:
// - The header and the footer name the company
// - The main link is a button with the email's own words, in the recipient's language
// - No "Vixly" anywhere in the message a company's customer receives
//
// IMPORTANT NOTES:
// - Uses the real resource files, so a text that says Vixly fails here
// - Written to a pickup folder; nothing is sent
// =========================================================
public sealed class OperationsMailSenderTests
{
    [Theory]
    [InlineData("en", "See my shipment")]
    [InlineData("fr", "Voir mon envoi")]
    [InlineData("es", "Ver mi envío")]
    public async Task CustomerEmail_IsDesignedUnderTheCompany_WithoutVixly(string language, string button)
    {
        string folder = Path.Combine(Path.GetTempPath(), "vixly-ops-mail-" + Guid.NewGuid().ToString("N"));
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();
        await using ServiceProvider provider = services.BuildServiceProvider();

        try
        {
            OperationsMailSender sender = new(provider.GetRequiredService<IStringLocalizer<OperationsText>>(), Options.Create(new OperationsMailOptions
            {
                FromAddress = "noreply@mail.test",
                PickupDirectory = folder
            }));
            Dictionary<string, object> values = new(StringComparer.Ordinal)
            {
                ["name"] = "Marie",
                ["shipment"] = "SHP-000002",
                ["place"] = "Les Cayes",
                ["address"] = "123 Rue Frere Rigaud",
                ["hours"] = "9:00-17:00",
                ["link"] = "https://galeo.test/customer/shipments/1",
                ["company"] = "Galeo Logistics"
            };

            await sender.SendAsync("marie@mail.test", "Marie", "Galeo Logistics", LanguageCode.Create(language),
                text => (VixlyText.Format(text, "email.customer.ready_for_pickup.subject", values), VixlyText.Format(text, "email.customer.ready_for_pickup.body", values)),
                CancellationToken.None);

            string message = await File.ReadAllTextAsync(Assert.Single(Directory.GetFiles(folder, "*.eml")));
            string html = System.Net.WebUtility.HtmlDecode(Decoded(message));

            Assert.Contains(">" + button + "</a>", html, StringComparison.Ordinal);
            Assert.Contains("Galeo Logistics", html, StringComparison.Ordinal);
            Assert.DoesNotContain("Vixly", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Vixly", message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }

    [Theory]
    [InlineData("#0b5fa5", "#0B5FA5")]
    [InlineData("#FACC15", null)]
    [InlineData("#FFFFFF", null)]
    [InlineData("not a colour", null)]
    [InlineData(null, null)]
    public void CompanyColour_IsUsedOnlyWhenWhiteTextStaysReadable(string? colour, string? expected)
    {
        Assert.Equal(expected, OperationsMailSender.Readable(colour));
    }

    // The pickup file holds each part base64 or quoted-printable; read the HTML part back.
    private static string Decoded(string message)
    {
        int start = message.IndexOf("Content-Type: text/html", StringComparison.Ordinal);
        Assert.True(start >= 0, "No HTML part.");
        int body = message.IndexOf("\r\n\r\n", start, StringComparison.Ordinal) + 4;
        int end = message.IndexOf("\r\n--", body, StringComparison.Ordinal);
        string part = message[body..end];
        string header = message[start..body];

        if (header.Contains("base64", StringComparison.OrdinalIgnoreCase))
        {
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(part.Replace("\r\n", string.Empty, StringComparison.Ordinal)));
        }

        string joined = part.Replace("=\r\n", string.Empty, StringComparison.Ordinal);
        List<byte> bytes = new();

        for (int at = 0; at < joined.Length; at++)
        {
            if (joined[at] == '=' && at + 2 < joined.Length)
            {
                bytes.Add(byte.Parse(joined.AsSpan(at + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                at += 2;
            }
            else
            {
                bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(joined[at].ToString()));
            }
        }

        return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
    }
}
