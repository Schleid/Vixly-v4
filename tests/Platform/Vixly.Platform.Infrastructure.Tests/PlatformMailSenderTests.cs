using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Vixly.Platform.Infrastructure.Mail;
using Vixly.Platform.Resources;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformMailSenderTests.cs
//
// PURPOSE:
// Checks the mail sender against the pickup directory.
//
// RESPONSIBILITIES:
// - An address with non-ASCII characters (lucía@...) is
//   written, not refused
//
// IMPORTANT NOTES:
// - Found in the Slice 5 run: a refused address failed the
//   outbox message on every attempt and blocked the tenant's
//   stream behind it
// =========================================================
public sealed class PlatformMailSenderTests
{
    [Fact]
    public async Task Sends_to_an_address_with_non_ascii_characters()
    {
        string folder = Path.Combine(Path.GetTempPath(), "vixly-mail-" + Guid.NewGuid().ToString("N"));

        try
        {
            PlatformMailSender sender = new(new KeyText(), Options.Create(new PlatformMailOptions
            {
                FromAddress = "billing@vixly.test",
                FromName = "Vixly",
                PickupDirectory = folder
            }));

            await sender.SendAsync("lucía@atlas-messagerie.test", "Lucía", LanguageCode.Create("es"), text => ("Factura", "Hola"), CancellationToken.None);

            string file = Assert.Single(Directory.GetFiles(folder, "*.eml"));
            Assert.Contains("lucía@atlas-messagerie.test", await File.ReadAllTextAsync(file), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }

    private sealed class KeyText : IStringLocalizer<PlatformText>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
    }
}
