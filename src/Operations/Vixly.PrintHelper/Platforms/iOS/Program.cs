using UIKit;

namespace Vixly.PrintHelper;

// =========================================================
// FILE: Program.cs
//
// PURPOSE:
// The iOS entry point of the print helper (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Start the app with its delegate
//
// IMPORTANT NOTES:
// - Standard MAUI start-up
// =========================================================
public static class Program
{
    private static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}
