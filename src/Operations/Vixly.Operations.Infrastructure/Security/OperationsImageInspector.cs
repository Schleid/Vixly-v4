using System.Buffers.Binary;
using System.Security.Cryptography;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Security;

// =========================================================
// FILE: OperationsImageInspector.cs
//
// PURPOSE:
// Recognizes PNG, JPEG and WebP from their bytes and reads their size (Slice 1B).
//
// RESPONSIBILITIES:
// - Check the file signature
// - Read the dimensions from the header
// - Hash the content
//
// IMPORTANT NOTES:
// - Never trusts the file name or the browser type
// - SVG, HTML and anything else return null
// =========================================================
public sealed class OperationsImageInspector : IOperationsImageInspector
{
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public OperationsImageInfo? Inspect(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        (string Type, int Width, int Height)? found = ReadPng(content) ?? ReadJpeg(content) ?? ReadWebp(content);
        return found is { } image && image.Width > 0 && image.Height > 0
            ? new OperationsImageInfo(image.Type, image.Width, image.Height, Convert.ToHexStringLower(SHA256.HashData(content)))
            : null;
    }

    private static (string, int, int)? ReadPng(byte[] data)
    {
        if (data.Length < 24 || !data.AsSpan(0, 8).SequenceEqual(Png) || data[12] != (byte)'I' || data[13] != (byte)'H' || data[14] != (byte)'D' || data[15] != (byte)'R')
        {
            return null;
        }

        return ("image/png", (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(16)), (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(20)));
    }

    private static (string, int, int)? ReadJpeg(byte[] data)
    {
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
        {
            return null;
        }

        int index = 2;

        while (index + 9 < data.Length)
        {
            if (data[index] != 0xFF)
            {
                return null;
            }

            byte marker = data[index + 1];
            int length = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(index + 2));

            // Start-of-frame markers carry the size; C4, C8 and CC are other tables.
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                return ("image/jpeg", BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(index + 7)), BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(index + 5)));
            }

            if (length < 2)
            {
                return null;
            }

            index += 2 + length;
        }

        return null;
    }

    private static (string, int, int)? ReadWebp(byte[] data)
    {
        if (data.Length < 30 || data[0] != (byte)'R' || data[1] != (byte)'I' || data[2] != (byte)'F' || data[3] != (byte)'F'
            || data[8] != (byte)'W' || data[9] != (byte)'E' || data[10] != (byte)'B' || data[11] != (byte)'P')
        {
            return null;
        }

        string chunk = System.Text.Encoding.ASCII.GetString(data, 12, 4);

        return chunk switch
        {
            "VP8X" => ("image/webp", 1 + (data[24] | (data[25] << 8) | (data[26] << 16)), 1 + (data[27] | (data[28] << 8) | (data[29] << 16))),
            "VP8L" when data[20] == 0x2F => ("image/webp", 1 + (data[21] | ((data[22] & 0x3F) << 8)), 1 + (((data[22] & 0xC0) >> 6) | (data[23] << 2) | ((data[24] & 0x0F) << 10))),
            "VP8 " when data[23] == 0x9D && data[24] == 0x01 && data[25] == 0x2A => ("image/webp", BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(26)) & 0x3FFF, BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(28)) & 0x3FFF),
            _ => null
        };
    }
}
