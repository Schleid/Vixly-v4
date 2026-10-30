namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsImageInspector.cs
//
// PURPOSE:
// Reads what an uploaded image really is (Slice 1B).
//
// RESPONSIBILITIES:
// - Recognize PNG, JPEG and WebP from their bytes and read their dimensions
//
// IMPORTANT NOTES:
// - Anything else (SVG, HTML, unknown) returns null
// =========================================================
public interface IOperationsImageInspector
{
    OperationsImageInfo? Inspect(byte[] content);
}
