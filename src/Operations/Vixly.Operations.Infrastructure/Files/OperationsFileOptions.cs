namespace Vixly.Operations.Infrastructure.Files;

// =========================================================
// FILE: OperationsFileOptions.cs
//
// PURPOSE:
// Where Operations keeps files such as parcel photos (D-R7, owner Q-R4).
//
// RESPONSIBILITIES:
// - Bind the OperationsFiles configuration section
//
// IMPORTANT NOTES:
// - Root is the folder of the default store; each tenant may later point at its own cloud storage
// - Keep the folder outside the web root and back it up with the database; Operations.Web and Operations.Api must name the same folder
// =========================================================
public sealed class OperationsFileOptions
{
    public const string SectionName = "OperationsFiles";

    public string Root { get; set; } = string.Empty;
}
