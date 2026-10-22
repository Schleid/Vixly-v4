namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsDevelopmentOptions.cs
//
// PURPOSE:
// Developer-computer settings of Operations.Web (section OperationsDevelopment, Slice 2).
//
// RESPONSIBILITIES:
// - Where Customer.Web runs, for the /customer pass-through
//
// IMPORTANT NOTES:
// - Read only when the environment is Development
// =========================================================
public sealed class OperationsDevelopmentOptions
{
    public const string SectionName = "OperationsDevelopment";

    public string CustomerPortalUrl { get; set; } = string.Empty;
}
