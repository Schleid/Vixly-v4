namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformSalesLeadLimits.cs
//
// PURPOSE:
// Sizes and retention of contact requests (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Field lengths and how long closed leads are kept
//
// IMPORTANT NOTES:
// - Closed leads are deleted 24 months after closing (privacy)
// =========================================================
public static class PlatformSalesLeadLimits
{
    public const int CompanyMaxLength = 200;

    public const int NameMaxLength = 120;

    public const int PhoneMaxLength = 40;

    public const int MessageMaxLength = 2000;

    public const int NoteMaxLength = 1000;

    public const int SourceMaxLength = 100;

    public const int IpAddressMaxLength = 45;

    public static readonly TimeSpan ClosedRetention = TimeSpan.FromDays(730);
}
