// =========================================================
// FILE: new-caller-key.cs
//
// PURPOSE:
// Makes a P-256 key pair for one caller between the V4 sites
// (Operations -> Platform, customer portal -> Operations,
// public site -> Platform), as docs/global/SETTINGS.md says.
//
// HOW TO RUN (.NET 10 runs a single file):
//   dotnet run build/tools/new-caller-key.cs -- ops-2026-10
//
// IMPORTANT NOTES:
// - The private key is printed once to this console only. Put it
//   straight into the caller's environment variable and do not
//   save it in any file that is copied or committed.
// - The private key is printed as 64 hexadecimal characters (0-9, a-f):
//   hosting panels keep them intact, unlike base64's + / = (2026-10-24).
// - The public key and the key id go in the called site's
//   Callers section (they are not secrets).
// =========================================================
using System.Security.Cryptography;

string keyId = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? args[0].Trim() : "key-" + DateTime.UtcNow.ToString("yyyy-MM");

using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

Console.WriteLine("Key id (both sides):");
Console.WriteLine("  " + keyId);
Console.WriteLine();
Console.WriteLine("PUBLIC key, for the called site's Callers:<caller>:Keys:" + keyId + " (not a secret):");
Console.WriteLine("  " + Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()));
Console.WriteLine();
Console.WriteLine("PRIVATE key, for the caller's ...:CallerPrivateKey environment variable (SECRET, shown once, 64 characters):");
Console.WriteLine("  " + Convert.ToHexString(key.ExportParameters(true).D!).ToLowerInvariant());
