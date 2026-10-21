namespace Vixly.Operations.Domain.Customers;

// =========================================================
// FILE: OperationsCustomerLimits.cs
//
// PURPOSE:
// Sizes and lifetimes for customers (Slice 2).
//
// RESPONSIBILITIES:
// - One place for column sizes, lifetimes and lockout
//
// IMPORTANT NOTES:
// - Sessions: D-C3
// =========================================================
public static class OperationsCustomerLimits
{
    public const int NameMaxLength = 100;

    public const int CompanyMaxLength = 150;

    public const int CodeMaxLength = 20;

    public const int PhoneMaxLength = 20;

    public const int ReasonMaxLength = 300;

    public const int MaxFailedSignIns = 5;

    public const int CodeDigits = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public static readonly TimeSpan SessionIdleTimeout = TimeSpan.FromMinutes(60);

    public static readonly TimeSpan SessionAbsoluteLifetime = TimeSpan.FromHours(12);

    public static TimeSpan LinkLifetime(OperationsCustomerTokenPurpose purpose) => purpose switch
    {
        OperationsCustomerTokenPurpose.Verification => TimeSpan.FromHours(24),
        _ => TimeSpan.FromMinutes(60)
    };
}
