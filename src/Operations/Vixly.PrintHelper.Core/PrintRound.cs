namespace Vixly.PrintHelper.Core;

// =========================================================
// FILE: PrintRound.cs
//
// PURPOSE:
// What one round of the print helper did (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Count the labels printed and failed; say whether the phone was stopped
//
// IMPORTANT NOTES:
// - Stopped: the app forgets its key and asks to join again
// =========================================================
public sealed record PrintRound(int Printed, int Failed, bool Stopped);
