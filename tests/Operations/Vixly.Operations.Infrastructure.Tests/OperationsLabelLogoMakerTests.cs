using System.Buffers.Binary;
using System.IO.Compression;
using Vixly.Operations.Infrastructure.Documents;
using Vixly.Operations.Infrastructure.Security;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsLabelLogoMakerTests.cs
//
// PURPOSE:
// Proves the black and white label copy of the document logo (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Coloured and dark ink prints black; white, very pale and transparent areas stay white
// - Palette PNGs are read; interlaced or damaged files are refused
// - The copy is a one-bit PNG at most 240 pixels tall, keeping the logo's proportions
//
// IMPORTANT NOTES:
// - The PNGs are written here, so every pixel is known
// =========================================================
public sealed class OperationsLabelLogoMakerTests
{
    private readonly OperationsLabelLogoMaker _maker = new();

    [Fact]
    public void ColouredInk_IsBlack_WhiteAndTransparent_StayWhite()
    {
        // Left third blue, middle third orange, right third white; the bottom row transparent.
        byte[] png = Rgba(30, 10, (x, y) => y == 9 ? (0, 0, 0, 0) : x < 10 ? (20, 40, 200, 255) : x < 20 ? (250, 160, 10, 255) : (255, 255, 255, 255));

        byte[] copy = _maker.MakeLabelCopy(png)!;
        bool[,] ink = Read(copy, out int width, out int height);

        Assert.Equal((30, 10), (width, height));
        Assert.True(ink[0, 0]);
        Assert.True(ink[15, 5]);
        Assert.False(ink[25, 5]);
        Assert.False(ink[5, 9]);
        Assert.Equal("image/png", new OperationsImageInspector().Inspect(copy)?.ContentType);
    }

    [Fact]
    public void TallLogos_AreMadeSmaller_KeepingTheirShape()
    {
        byte[] png = Rgba(800, 480, (x, y) => x < 400 ? (0, 0, 0, 255) : (255, 255, 255, 255));

        bool[,] ink = Read(_maker.MakeLabelCopy(png)!, out int width, out int height);

        Assert.Equal((400, 240), (width, height));
        Assert.True(ink[10, 100]);
        Assert.False(ink[390, 100]);
    }

    [Fact]
    public void PaletteLogos_AreRead_DamagedOrInterlacedOnes_Refused()
    {
        // Two colours: index 0 white, index 1 navy, 8 bits per pixel.
        byte[] palette = Png(4, 2, 3, 8, new byte[] { 0, 1, 1, 0, 0, 0, 1, 1 }, new byte[] { 255, 255, 255, 0, 0, 128 });
        bool[,] ink = Read(_maker.MakeLabelCopy(palette)!, out int _, out int _);
        Assert.False(ink[0, 0]);
        Assert.True(ink[1, 0]);
        Assert.True(ink[3, 1]);

        byte[] interlaced = Rgba(4, 4, (_, _) => (0, 0, 0, 255));
        interlaced[28] = 1;
        Assert.Null(_maker.MakeLabelCopy(interlaced));
        Assert.Null(_maker.MakeLabelCopy(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }));
        Assert.Null(_maker.MakeLabelCopy(Rgba(4, 4, (_, _) => (0, 0, 0, 255))[..40]));
    }

    [Fact]
    public void LabelCopy_BecomesAZplGraphic_ForThermalPrinters()
    {
        // Left half black, right half white: each 64-dot row is 8 bytes, the first four all ink.
        byte[] copy = _maker.MakeLabelCopy(Rgba(128, 60, (x, _) => x < 64 ? (0, 0, 0, 255) : (255, 255, 255, 255)))!;

        string graphic = Vixly.Operations.Application.Printing.OperationsZplLogo.Graphic(copy, 30, 732)!;

        Assert.StartsWith("^GFA,240,240,8,", graphic, StringComparison.Ordinal);
        Assert.StartsWith("FFFFFFFF00000000", graphic[15..], StringComparison.Ordinal);
        Assert.Matches("^[0-9A-F]+$", graphic[15..]);
        Assert.Null(Vixly.Operations.Application.Printing.OperationsZplLogo.Graphic(Rgba(8, 8, (_, _) => (0, 0, 0, 255)), 30, 732));
        Assert.Null(Vixly.Operations.Application.Printing.OperationsZplLogo.Graphic(null, 30, 732));
    }

    // A PNG of 8-bit RGBA pixels.
    internal static byte[] Rgba(int width, int height, Func<int, int, (int R, int G, int B, int A)> pixel)
    {
        byte[] raw = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                (int r, int g, int b, int a) = pixel(x, y);
                int at = (y * width + x) * 4;
                (raw[at], raw[at + 1], raw[at + 2], raw[at + 3]) = ((byte)r, (byte)g, (byte)b, (byte)a);
            }
        }

        return Png(width, height, 6, 8, raw, null);
    }

    private static byte[] Png(int width, int height, byte colour, byte depth, byte[] pixels, byte[]? palette)
    {
        int stride = pixels.Length / height;
        using MemoryStream raw = new();

        for (int y = 0; y < height; y++)
        {
            raw.WriteByte(0);
            raw.Write(pixels, y * stride, stride);
        }

        using MemoryStream packed = new();

        using (ZLibStream deflate = new(packed, CompressionLevel.Fastest, leaveOpen: true))
        {
            deflate.Write(raw.ToArray());
        }

        byte[] header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        (header[8], header[9]) = (depth, colour);
        using MemoryStream png = new();
        png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        Chunk(png, "IHDR", header);

        if (palette is not null)
        {
            Chunk(png, "PLTE", palette);
        }

        Chunk(png, "IDAT", packed.ToArray());
        Chunk(png, "IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    private static void Chunk(Stream output, string type, byte[] content)
    {
        byte[] length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, content.Length);
        byte[] name = System.Text.Encoding.ASCII.GetBytes(type);
        output.Write(length);
        output.Write(name);
        output.Write(content);
        byte[] check = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(check, Crc(name.Concat(content)));
        output.Write(check);
    }

    // The PNG checksum, so the PDF engine reads these files too.
    internal static uint Crc(IEnumerable<byte> bytes)
    {
        uint crc = 0xFFFFFFFFu;

        foreach (byte value in bytes)
        {
            crc ^= value;

            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
            }
        }

        return crc ^ 0xFFFFFFFFu;
    }

    // The one-bit copy: true where it prints black.
    private static bool[,] Read(byte[] png, out int width, out int height)
    {
        width = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16));
        height = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20));
        Assert.Equal((1, 0), (png[24], png[25]));
        int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(33));
        Assert.Equal("IDAT", System.Text.Encoding.ASCII.GetString(png, 37, 4));
        int stride = (width + 7) / 8;
        byte[] raw = new byte[(stride + 1) * height];

        using (ZLibStream inflate = new(new MemoryStream(png, 41, length), CompressionMode.Decompress))
        {
            inflate.ReadExactly(raw);
        }

        bool[,] ink = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                ink[x, y] = (raw[y * (stride + 1) + 1 + x / 8] & (0x80 >> (x % 8))) == 0;
            }
        }

        return ink;
    }
}
