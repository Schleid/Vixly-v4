using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Vixly.Operations.Application.Printing;

// =========================================================
// FILE: OperationsZplLogo.cs
//
// PURPOSE:
// The company's black and white label logo as a ZPL graphic, for shipping labels sent to a thermal printer (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Read the one-bit PNG Vixly made at upload (grey, one bit, no filter per row)
// - Scale it to the height asked, in printer dots, at most the label's width
// - Write it as ^GFA (black is 1 in ZPL, 0 in the PNG)
//
// IMPORTANT NOTES:
// - Any other file gives null: the label then prints the company's name, as before
// - Hex digits only: nothing in it can carry a printer command
// =========================================================
public static class OperationsZplLogo
{
    public static string? Graphic(byte[]? png, int heightDots, int maxWidthDots)
    {
        if (png is null || png.Length < 45 || heightDots < 8 || maxWidthDots < 8 || png[24] != 1 || png[25] != 0)
        {
            return null;
        }

        try
        {
            int width = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4));
            int height = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4));

            if (width is < 1 or > 4000 || height is < 1 or > 4000)
            {
                return null;
            }

            using MemoryStream data = new();

            for (int at = 8; at + 12 <= png.Length;)
            {
                int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(at, 4));

                if (length < 0 || at + 12 + length > png.Length)
                {
                    return null;
                }

                if (png.AsSpan(at + 4, 4).SequenceEqual("IDAT"u8))
                {
                    data.Write(png, at + 8, length);
                }

                at += 12 + length;
            }

            int stride = (width + 7) / 8;
            byte[] raw = new byte[(stride + 1) * height];
            data.Position = 0;

            using (ZLibStream inflate = new(data, CompressionMode.Decompress))
            {
                inflate.ReadExactly(raw);
            }

            bool Ink(int x, int y)
            {
                int row = y * (stride + 1);
                return raw[row] == 0 && (raw[row + 1 + x / 8] & (0x80 >> (x % 8))) == 0;
            }

            int newHeight = heightDots;
            int newWidth = Math.Min(maxWidthDots, Math.Max(1, (int)Math.Round(width * (double)heightDots / height)));
            int bytesPerRow = (newWidth + 7) / 8;
            StringBuilder hex = new(bytesPerRow * newHeight * 2);

            for (int y = 0; y < newHeight; y++)
            {
                int sourceY = Math.Min(height - 1, y * height / newHeight);

                for (int column = 0; column < bytesPerRow; column++)
                {
                    int value = 0;

                    for (int bit = 0; bit < 8; bit++)
                    {
                        int x = column * 8 + bit;

                        if (x < newWidth && Ink(Math.Min(width - 1, (int)((long)x * height / newHeight)), sourceY))
                        {
                            value |= 0x80 >> bit;
                        }
                    }

                    hex.Append(value.ToString("X2", CultureInfo.InvariantCulture));
                }
            }

            int total = bytesPerRow * newHeight;
            return string.Create(CultureInfo.InvariantCulture, $"^GFA,{total},{total},{bytesPerRow},{hex}");
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or ArgumentException or IndexOutOfRangeException)
        {
            return null;
        }
    }
}
