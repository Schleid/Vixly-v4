using System.Buffers.Binary;
using System.IO.Compression;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Documents;

// =========================================================
// FILE: OperationsLabelLogoMaker.cs
//
// PURPOSE:
// The black and white copy of the document logo for thermal labels (owner, 2026-10-28: "industry standard").
//
// RESPONSIBILITIES:
// - Decode the PNG (grey, colour, palette, with or without transparency; 1 to 16 bits; not interlaced)
// - Lay it on white, make it at most MaxHeight pixels tall, and turn every pixel black or white
// - Write a one-bit PNG
//
// IMPORTANT NOTES:
// - Coloured ink (blue, orange...) prints black, so no part of the logo disappears; only white and very pale areas stay white
// - No image library: plain PNG reading with the framework's zlib
// =========================================================
internal sealed class OperationsLabelLogoMaker : IOperationsLabelLogoMaker
{
    // About 1.2 inches at 203 dpi: sharp on a 4 x 6 label, small to send to the printer.
    public const int MaxHeight = 240;

    // Lighter than this (on 255) after laying on white stays white.
    public const int InkThreshold = 200;

    private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private static readonly uint[] CrcTable = MakeCrcTable();

    public byte[]? MakeLabelCopy(byte[] png)
    {
        ArgumentNullException.ThrowIfNull(png);

        try
        {
            // The PDF engine must read it too, or no document could be drawn with it.
            _ = QuestPDF.Infrastructure.Image.FromBinaryData(png);
            return Decode(png) is { } image ? Encode(BlackAndWhite(image.Lightness, image.Width, image.Height)) : null;
        }
        catch (QuestPDF.Drawing.Exceptions.DocumentComposeException)
        {
            return null;
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or IndexOutOfRangeException or ArgumentException or OverflowException)
        {
            return null;
        }
    }

    // Lightness of each pixel laid on white, 0 (black) to 255 (white).
    private static (byte[] Lightness, int Width, int Height)? Decode(byte[] png)
    {
        if (png.Length < 33 || !png.AsSpan(0, 8).SequenceEqual(Signature))
        {
            return null;
        }

        int width = 0, height = 0, depth = 0, colour = -1;
        byte[]? palette = null, transparency = null;
        using MemoryStream data = new();
        int at = 8;

        while (at + 8 <= png.Length)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(at, 4));
            string type = System.Text.Encoding.ASCII.GetString(png, at + 4, 4);

            if (length < 0 || at + 12 + length > png.Length)
            {
                return null;
            }

            ReadOnlySpan<byte> chunk = png.AsSpan(at + 8, length);

            switch (type)
            {
                case "IHDR":
                    width = BinaryPrimitives.ReadInt32BigEndian(chunk[..4]);
                    height = BinaryPrimitives.ReadInt32BigEndian(chunk.Slice(4, 4));
                    (depth, colour) = (chunk[8], chunk[9]);

                    if (chunk[12] != 0)
                    {
                        return null;
                    }

                    break;
                case "PLTE":
                    palette = chunk.ToArray();
                    break;
                case "tRNS":
                    transparency = chunk.ToArray();
                    break;
                case "IDAT":
                    data.Write(chunk);
                    break;
            }

            if (type == "IEND")
            {
                break;
            }

            at += 12 + length;
        }

        int channels = colour switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, 6 => 4, _ => 0 };

        if (width is < 1 or > 4000 || height is < 1 or > 4000 || channels == 0 || depth is not (1 or 2 or 4 or 8 or 16) || (colour == 3 && palette is null))
        {
            return null;
        }

        int bitsPerPixel = channels * depth;
        int bytesPerPixel = Math.Max(1, bitsPerPixel / 8);
        int stride = (width * bitsPerPixel + 7) / 8;
        byte[] raw = new byte[(stride + 1) * height];
        data.Position = 0;

        using (ZLibStream inflate = new(data, CompressionMode.Decompress))
        {
            inflate.ReadExactly(raw);
        }

        byte[] previous = new byte[stride];
        byte[] row = new byte[stride];
        byte[] lightness = new byte[width * height];

        for (int y = 0; y < height; y++)
        {
            int filter = raw[y * (stride + 1)];
            Array.Copy(raw, y * (stride + 1) + 1, row, 0, stride);
            Unfilter(filter, row, previous, bytesPerPixel);

            for (int x = 0; x < width; x++)
            {
                lightness[y * width + x] = Pixel(row, x, colour, depth, palette, transparency);
            }

            (previous, row) = (row, previous);
        }

        return (lightness, width, height);
    }

    private static void Unfilter(int filter, byte[] row, byte[] previous, int bytesPerPixel)
    {
        for (int index = 0; index < row.Length; index++)
        {
            int left = index >= bytesPerPixel ? row[index - bytesPerPixel] : 0;
            int up = previous[index];
            int upLeft = index >= bytesPerPixel ? previous[index - bytesPerPixel] : 0;
            row[index] = (byte)(row[index] + filter switch
            {
                0 => 0,
                1 => left,
                2 => up,
                3 => (left + up) / 2,
                4 => Paeth(left, up, upLeft),
                _ => throw new InvalidDataException("Unknown PNG filter.")
            });
        }
    }

    private static int Paeth(int left, int up, int upLeft)
    {
        int estimate = left + up - upLeft;
        int toLeft = Math.Abs(estimate - left), toUp = Math.Abs(estimate - up), toUpLeft = Math.Abs(estimate - upLeft);
        return toLeft <= toUp && toLeft <= toUpLeft ? left : toUp <= toUpLeft ? up : upLeft;
    }

    // One pixel: its colour laid on white, as lightness 0 to 255.
    private static byte Pixel(byte[] row, int x, int colour, int depth, byte[]? palette, byte[]? transparency)
    {
        int Sample(int channel, int channels)
        {
            if (depth == 16)
            {
                return row[(x * channels + channel) * 2];
            }

            if (depth == 8)
            {
                return row[x * channels + channel];
            }

            int bit = x * depth;
            int value = (row[bit / 8] >> (8 - depth - bit % 8)) & ((1 << depth) - 1);
            return colour == 3 ? value : value * 255 / ((1 << depth) - 1);
        }

        int red, green, blue, alpha = 255;

        switch (colour)
        {
            case 0:
                red = green = blue = Sample(0, 1);
                break;
            case 2:
                (red, green, blue) = (Sample(0, 3), Sample(1, 3), Sample(2, 3));
                break;
            case 3:
                int entry = Sample(0, 1);
                (red, green, blue) = entry * 3 + 2 < palette!.Length ? (palette[entry * 3], palette[entry * 3 + 1], palette[entry * 3 + 2]) : (255, 255, 255);
                alpha = transparency is not null && entry < transparency.Length ? transparency[entry] : 255;
                break;
            case 4:
                (red, alpha) = (Sample(0, 2), Sample(1, 2));
                green = blue = red;
                break;
            default:
                (red, green, blue, alpha) = (Sample(0, 4), Sample(1, 4), Sample(2, 4), Sample(3, 4));
                break;
        }

        int light = (299 * red + 587 * green + 114 * blue) / 1000;
        return (byte)((light * alpha + 255 * (255 - alpha)) / 255);
    }

    // At most MaxHeight tall (each new pixel the average of the ones it covers), then black or white.
    private static (bool[] Ink, int Width, int Height) BlackAndWhite(byte[] lightness, int width, int height)
    {
        int newHeight = Math.Min(height, MaxHeight);
        int newWidth = Math.Max(1, (int)Math.Round(width * (double)newHeight / height));
        bool[] ink = new bool[newWidth * newHeight];

        for (int y = 0; y < newHeight; y++)
        {
            int top = y * height / newHeight, bottom = Math.Max(top + 1, (y + 1) * height / newHeight);

            for (int x = 0; x < newWidth; x++)
            {
                int left = x * width / newWidth, right = Math.Max(left + 1, (x + 1) * width / newWidth);
                long sum = 0;

                for (int sy = top; sy < bottom; sy++)
                {
                    for (int sx = left; sx < right; sx++)
                    {
                        sum += lightness[sy * width + sx];
                    }
                }

                ink[y * newWidth + x] = sum / ((bottom - top) * (right - left)) < InkThreshold;
            }
        }

        return (ink, newWidth, newHeight);
    }

    // A grey one-bit PNG: 0 is black.
    private static byte[] Encode((bool[] Ink, int Width, int Height) image)
    {
        int stride = (image.Width + 7) / 8;
        byte[] raw = new byte[(stride + 1) * image.Height];

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                if (!image.Ink[y * image.Width + x])
                {
                    raw[y * (stride + 1) + 1 + x / 8] |= (byte)(0x80 >> (x % 8));
                }
            }
        }

        using MemoryStream compressed = new();

        using (ZLibStream deflate = new(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(raw);
        }

        byte[] header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, image.Width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), image.Height);
        header[8] = 1;
        using MemoryStream png = new();
        png.Write(Signature);
        Chunk(png, "IHDR", header);
        Chunk(png, "IDAT", compressed.ToArray());
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
        uint crc = 0xFFFFFFFFu;

        foreach (byte value in name.Concat(content))
        {
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        byte[] check = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(check, crc ^ 0xFFFFFFFFu);
        output.Write(check);
    }

    private static uint[] MakeCrcTable()
    {
        uint[] table = new uint[256];

        for (uint index = 0; index < 256; index++)
        {
            uint value = index;

            for (int bit = 0; bit < 8; bit++)
            {
                value = (value & 1) != 0 ? 0xEDB88320u ^ (value >> 1) : value >> 1;
            }

            table[index] = value;
        }

        return table;
    }
}
