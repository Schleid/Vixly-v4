namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsLabelLogoMaker.cs
//
// PURPOSE:
// Makes the black and white copy of the document logo for thermal labels (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Read the PNG; every coloured or dark pixel becomes black, white and pale ones white
//
// IMPORTANT NOTES:
// - Null when the PNG cannot be read (interlaced or damaged): the caller refuses the upload
// =========================================================
public interface IOperationsLabelLogoMaker
{
    byte[]? MakeLabelCopy(byte[] png);
}
