using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: FixtureAction.cs
//
// PURPOSE:
// Test-only child entity of FixtureCase.
//
// RESPONSIBILITIES:
// - Carry a code-assigned Id from AuditableEntity
//
// IMPORTANT NOTES:
// - Exists only to prove the D-5 convention
// =========================================================
internal sealed class FixtureAction : AuditableEntity
{
    private FixtureAction()
    {
    }

    public FixtureAction(DateTimeOffset createdUtc)
        : base(createdUtc)
    {
    }
}
