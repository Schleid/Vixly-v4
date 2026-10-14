using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: FixtureCase.cs
//
// PURPOSE:
// Test-only aggregate shaped like a collection case: a parent
// that records actions as children.
//
// RESPONSIBILITIES:
// - Own a list of FixtureAction children
// - Add a child with a code-assigned Id
//
// IMPORTANT NOTES:
// - Exists only to prove the D-5 convention
// =========================================================
internal sealed class FixtureCase : AuditableEntity
{
    private readonly List<FixtureAction> _actions = new();

    private FixtureCase()
    {
    }

    public FixtureCase(DateTimeOffset createdUtc)
        : base(createdUtc)
    {
    }

    public IReadOnlyList<FixtureAction> Actions => _actions;

    public FixtureAction AddAction(DateTimeOffset createdUtc)
    {
        FixtureAction action = new(createdUtc);
        _actions.Add(action);

        return action;
    }
}
