using System.Security.Cryptography;

namespace Vixly.Operations.Application.Holds;

// =========================================================
// FILE: OperationsDocumentInspector.cs
//
// PURPOSE:
// Tells a PDF, PNG or JPEG file by its first bytes, never by its name (Slice 3E).
//
// RESPONSIBILITIES:
// - Name the file's type, or refuse it
// - Give its SHA-256 for the record
//
// IMPORTANT NOTES:
// - A file that claims to be a PDF but is not is refused
// - No active content is ever served back: staff download it as an attachment
// =========================================================
public static class OperationsDocumentInspector
{
    public static string? TypeOf(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Length >= 5 && content[0] == (byte)'%' && content[1] == (byte)'P' && content[2] == (byte)'D' && content[3] == (byte)'F' && content[4] == (byte)'-')
        {
            return "application/pdf";
        }

        if (content.Length >= 8 && content[0] == 0x89 && content[1] == (byte)'P' && content[2] == (byte)'N' && content[3] == (byte)'G')
        {
            return "image/png";
        }

        return content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF ? "image/jpeg" : null;
    }

    public static string Sha256(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));
}
