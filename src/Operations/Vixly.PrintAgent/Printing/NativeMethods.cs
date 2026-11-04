using System.Runtime.InteropServices;

namespace Vixly.PrintAgent.Printing;

// =========================================================
// FILE: NativeMethods.cs
//
// PURPOSE:
// The Windows print spooler functions used for raw printing (Slice 3B).
//
// RESPONSIBILITIES:
// - Declare the winspool.drv imports
//
// IMPORTANT NOTES:
// - Windows only; called through RawPrinting
// =========================================================
internal static class NativeMethods
{
    [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool OpenPrinter(string printerName, out IntPtr printer, IntPtr defaults);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool ClosePrinter(IntPtr printer);

    [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool StartDocPrinter(IntPtr printer, int level, [In] DocInfo document);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool EndDocPrinter(IntPtr printer);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool StartPagePrinter(IntPtr printer);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool EndPagePrinter(IntPtr printer);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool WritePrinter(IntPtr printer, IntPtr bytes, int count, out int written);
}
