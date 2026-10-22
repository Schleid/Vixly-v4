using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsIdentityText.cs
//
// PURPOSE:
// Validation of the free-text fields of the staff identity model.
//
// RESPONSIBILITIES:
// - Trim and check names
// - Trim optional values
//
// IMPORTANT NOTES:
// - Internal: only identity entities use it
// =========================================================
internal static class OperationsIdentityText
{
    public static string RequiredName(string? value)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException(OperationsIdentityErrors.NameRequired);
        }

        if (trimmed.Length > OperationsIdentityLimits.NameMaxLength)
        {
            throw new DomainException(OperationsIdentityErrors.NameTooLong);
        }

        return trimmed;
    }

    public static string? Clip(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
