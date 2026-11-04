namespace Vixly.PrintAgent.Printing;

// =========================================================
// FILE: PrintAgentOptions.cs
//
// PURPOSE:
// Settings of the dock print program (Slice 3B).
//
// RESPONSIBILITIES:
// - Bind the PrintAgent section
//
// IMPORTANT NOTES:
// - The key is a secret: user secrets or the service environment, never a shared file
// =========================================================
public sealed class PrintAgentOptions
{
    public const string SectionName = "PrintAgent";

    public string OperationsUrl { get; set; } = string.Empty;

    public string AgentKey { get; set; } = string.Empty;

    public int PollSeconds { get; set; } = 2;
}
