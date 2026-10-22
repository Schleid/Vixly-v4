namespace Vixly.Operations.Application.UseCases.Holds.ChangeOperationsUnclaimedDays;

// =========================================================
// FILE: ChangeOperationsUnclaimedDaysRequest.cs
//
// PURPOSE:
// Changes after how many days a waiting package goes to the decision list (Slice 3E, Q-R6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsUnclaimedDaysRequest
{
    public int Days { get; init; }
}
