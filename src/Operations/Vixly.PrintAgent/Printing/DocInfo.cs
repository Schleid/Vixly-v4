using System.Runtime.InteropServices;

namespace Vixly.PrintAgent.Printing;

// =========================================================
// FILE: DocInfo.cs
//
// PURPOSE:
// The DOC_INFO_1 structure of the Windows print spooler (Slice 3B).
//
// RESPONSIBILITIES:
// - Name the document and its data type (RAW)
//
// IMPORTANT NOTES:
// - Layout fixed by winspool.drv
// =========================================================
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal sealed class DocInfo
{
    [MarshalAs(UnmanagedType.LPWStr)]
    public string DocumentName = string.Empty;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string? OutputFile;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string DataType = "RAW";
}
