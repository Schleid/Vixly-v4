namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: IOperationsVersion.cs
//
// PURPOSE:
// A dated version of prices or terms (Slice 4B design section 5).
//
// RESPONSIBILITIES:
// - Name the version and the moment it starts
//
// IMPORTANT NOTES:
// - A version applies until the next one starts
// =========================================================
public interface IOperationsVersion
{
    Guid Id { get; }

    DateTimeOffset EffectiveFromUtc { get; }
}
