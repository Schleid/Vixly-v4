using System.Net.Mail;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: VixlyEmailTests.cs
//
// PURPOSE:
// Proves the designed email layout (Shared.Localization, owner 2026-10-25).
//
// RESPONSIBILITIES:
// - The main link becomes a button, with the same link below it to copy
// - A "Label: link" line becomes a button with that label
// - Text is encoded; links and addresses are clickable; "- " lines become a list
// - The message carries the plain text and the HTML, and the logo as an embedded image
// - The button words come from the email's own ".button" key
//
// IMPORTANT NOTES:
// - Runs without SMTP: the message is built, not sent
// =========================================================
public sealed class VixlyEmailTests
{
    private static readonly VixlyEmailBrand Brand = new("Vixly", "#2563EB", "#2563EB");

    private static readonly VixlyEmailWords Words = new("Open", "Copy this link:", new[] { "Questions? Write to help@vixly.test." });

    [Fact]
    public void LoneLink_BecomesAButton_WithTheLinkToCopy()
    {
        string html = VixlyEmail.Html("Invitation", "Hello Ada,\n\nOpen this link:\n\nhttps://control.test/invitation?token=a&b\n\nThanks", Brand, Words, "en", "Choose my password");

        Assert.Contains(">Choose my password</a>", html, StringComparison.Ordinal);
        Assert.Equal(2, CountOf(html, "href=\"https://control.test/invitation?token=a&amp;b\""));
        Assert.Contains("Copy this link:", html, StringComparison.Ordinal);
        Assert.Contains("<p style=\"margin:0 0 16px;\">Thanks</p>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ACompanyWideLogo_ShowsOnWhite_AtTheHeaderHeight_UnderItsColour()
    {
        VixlyEmailBrand brand = new("Galeo Logistics", "#0B5FA5", "#0B5FA5", null, new VixlyEmailLogo(new byte[] { 1 }, "image/png", 600, 200));
        VixlyEmailBrand tooWide = brand with { WideLogo = new VixlyEmailLogo(new byte[] { 1 }, "image/jpeg", 2000, 100) };

        string html = VixlyEmail.Html("Ready", "Hello\n\nhttps://galeo.test/c/1", brand, Words, "en", "See my shipment");

        Assert.Contains("border-top:6px solid #0B5FA5", html, StringComparison.Ordinal);
        Assert.Contains("width=\"132\" height=\"44\" alt=\"Galeo Logistics\"", html, StringComparison.Ordinal);
        Assert.Equal((240, 12), (tooWide.WideLogo!.HeaderWidth, tooWide.WideLogo.ShownHeight));
    }

    [Fact]
    public void LabelledLink_UsesItsLabel()
    {
        VixlyEmailAction? action = VixlyEmail.FindAction("Invoice attached.\nPay online: https://pay.test/p/1");

        Assert.Equal(new VixlyEmailAction("https://pay.test/p/1", "Pay online"), action);
        Assert.Contains(">Pay online</a>", VixlyEmail.Html("Invoice", "Invoice attached.\nPay online: https://pay.test/p/1", Brand, Words, "en", null), StringComparison.Ordinal);
    }

    [Fact]
    public void NoLink_NoButton_AndTextIsEncoded()
    {
        string html = VixlyEmail.Html("Notice", "Hello <b>Ada</b>,\n\n- one\n- two\n\nWrite to help@vixly.test.", Brand, Words, "en", null);

        Assert.DoesNotContain("Copy this link:", html, StringComparison.Ordinal);
        Assert.Contains("Hello &lt;b&gt;Ada&lt;/b&gt;,", html, StringComparison.Ordinal);
        Assert.Contains("<li style=\"margin:0 0 6px;\">two</li>", html, StringComparison.Ordinal);
        Assert.Contains("href=\"mailto:help@vixly.test\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Message_HasPlainAndHtml_AndTheLogoEmbedded()
    {
        using MailMessage message = new();
        VixlyEmail.Fill(message, "Subject", "Hello\n\nhttps://x.test/a", Brand with { LogoPng = new byte[] { 1, 2, 3 } }, Words, "en", null);

        Assert.Equal(new[] { "text/plain", "text/html" }, message.AlternateViews.Select(view => view.ContentType.MediaType).ToArray());
        Assert.Equal(VixlyEmail.LogoContentId, Assert.Single(message.AlternateViews[1].LinkedResources).ContentId);
    }

    [Fact]
    public void ButtonKey_FollowsTheBodyKey()
    {
        Assert.Equal("email.invitation.button", VixlyEmail.ButtonKey(new[] { "email.invitation.subject", "email.invitation.body" }));
        Assert.Equal("email.company_change.approved_button", VixlyEmail.ButtonKey(new[] { "email.company_change.approved_body" }));
        Assert.Null(VixlyEmail.ButtonKey(new[] { "email.notice.subject" }));
    }

    private static int CountOf(string text, string value)
    {
        int count = 0;

        for (int at = text.IndexOf(value, StringComparison.Ordinal); at >= 0; at = text.IndexOf(value, at + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
