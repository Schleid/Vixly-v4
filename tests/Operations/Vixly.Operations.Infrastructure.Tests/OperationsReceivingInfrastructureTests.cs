using Microsoft.Extensions.Options;
using Vixly.Operations.Infrastructure.Documents;
using Vixly.Operations.Infrastructure.Files;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsReceivingInfrastructureTests.cs
//
// PURPOSE:
// Proves the file store and the barcode of Slice 3B (D-R3, D-R7).
//
// RESPONSIBILITIES:
// - Files stay inside their tenant and are read only by a well-formed key
// - Code 128 follows the standard: start B, check symbol, stop
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsReceivingInfrastructureTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vixly-store-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Files_AreKeptPerTenant_AndReadOnlyByTheirKey()
    {
        OperationsFileStore store = Store();
        Guid tenant = Guid.NewGuid();
        byte[] content = { 1, 2, 3 };

        string key = await store.SaveAsync(tenant, "parcels", content, "image/jpeg", CancellationToken.None);

        Assert.Matches("^folder:parcels/[0-9]{4}/[0-9]{2}/[0-9a-f]{32}\\.jpg$", key);
        Assert.Equal(content, await store.ReadAsync(tenant, key, CancellationToken.None));
        Assert.True(File.Exists(Path.Combine(_root, tenant.ToString("N"), key["folder:".Length..])));

        // Another tenant, a made-up path or another provider reads nothing.
        Assert.Null(await store.ReadAsync(Guid.NewGuid(), key, CancellationToken.None));
        Assert.Null(await store.ReadAsync(tenant, "folder:../" + tenant.ToString("N") + "/x.jpg", CancellationToken.None));
        Assert.Null(await store.ReadAsync(tenant, "folder:parcels/2026/10/../../../secret.jpg", CancellationToken.None));
        Assert.Null(await store.ReadAsync(tenant, key.Replace("folder:", "azure:", StringComparison.Ordinal), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(tenant, "../up", content, "image/png", CancellationToken.None));

        // A refused command removes its file; a made-up key removes nothing.
        await store.DeleteAsync(tenant, "folder:../../etc/passwd", CancellationToken.None);
        await store.DeleteAsync(tenant, key, CancellationToken.None);
        Assert.Null(await store.ReadAsync(tenant, key, CancellationToken.None));
    }

    [Fact]
    public void Code128_EncodesSetB_WithItsCheckSymbol()
    {
        // ISO/IEC 15417 example arithmetic: start B (104) + each value times its position, modulo 103.
        Assert.Equal((104 + (48 * 1) + (42 * 2) + (42 * 3) + (17 * 4) + (18 * 5) + (19 * 6) + (35 * 7)) % 103, OperationsCode128.Checksum("PJJ123C"));

        IReadOnlyList<int> widths = OperationsCode128.Widths("PK26000001");
        Assert.Equal((11 * (10 + 2)) + 13, widths.Sum());
        Assert.Equal(new[] { 2, 1, 1, 2, 1, 4 }, widths.Take(6));
        Assert.Equal(new[] { 2, 3, 3, 1, 1, 1, 2 }, widths.TakeLast(7));
        Assert.Throws<ArgumentException>(() => OperationsCode128.Widths("é"));
    }

    [Fact]
    public void Label_IsAPdf()
    {
        byte[] pdf = Renderer().RenderParcelLabel(new Vixly.Operations.Application.Ports.OperationsParcelLabel("PK26000001", null, "Harbour Freight", "WH1", DateTimeOffset.UtcNow));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void ShippingLabel_IsAPdf()
    {
        byte[] pdf = Renderer().RenderShippingLabel(new Vixly.Operations.Application.Ports.OperationsShippingLabel("Harbour Freight", "SH26000001", "PK26000001", 1, 2, "MIA",
            "JER", "ocean_consolidated", "Ana Pierre", "Jérémie", false, "12 lb"));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    // A long delivery runs over several pages.
    [Fact]
    public void Receipt_IsAPdf_OverSeveralPages()
    {
        List<Vixly.Operations.Application.Ports.OperationsReceiptLine> lines = Enumerable.Range(1, 80)
            .Select(index => new Vixly.Operations.Application.Ports.OperationsReceiptLine("PK260000" + index.ToString("00", System.Globalization.CultureInfo.InvariantCulture), "1Z999AA1012345678" + (index % 10), index % 3 == 0 ? null : "C00042", index == 7))
            .ToList();
        byte[] pdf = Renderer().RenderReceipt(new Vixly.Operations.Application.Ports.OperationsReceiptDocument("Harbour Freight", "WH1", "Doral", "WR26000001", "UPS",
            DateTimeOffset.UtcNow.AddMinutes(-30), DateTimeOffset.UtcNow, "Grace Hopper", 80, 1, lines));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    private static QuestPdfOperationsLabelRenderer Renderer() =>
        new(Options.Create(new OperationsDocumentOptions()),
            new Microsoft.Extensions.Localization.StringLocalizer<Vixly.Operations.Resources.OperationsText>(new Microsoft.Extensions.Localization.ResourceManagerStringLocalizerFactory(
                Options.Create(new Microsoft.Extensions.Localization.LocalizationOptions()), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance)));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    private OperationsFileStore Store() =>
        new(new[] { new OperationsFolderFileProvider(Options.Create(new OperationsFileOptions { Root = _root })) }, TimeProvider.System);
}
