namespace Vixly.Operations.Domain.Inspection;

// =========================================================
// FILE: OperationsInspectionLimits.cs
//
// PURPOSE:
// Sizes of the restricted list, the rules and the inspection record (Slice 3C).
//
// RESPONSIBILITIES:
// - Keep the database columns and the forms in step
//
// IMPORTANT NOTES:
// - Changing one needs a migration
// =========================================================
public static class OperationsInspectionLimits
{
    public const int NameMaxLength = 100;

    public const int WordsMaxLength = 500;

    public const int MaxWords = 30;

    public const int CountriesMaxLength = 300;

    public const int CarriersMaxLength = 300;

    public const int RequirementMaxLength = 500;

    public const int RuleValueMaxLength = 60;

    public const int ReasonMaxLength = 200;

    public const int NotesMaxLength = 1000;

    public const int CustomerActionMaxLength = 500;

    public const int ItemsFoundMaxLength = 1000;

    public const int CallNotesMaxLength = 500;

    public const decimal MaxFee = 100000m;

    public const int MaxItems = 200;

    public const int MaxRules = 100;
}
