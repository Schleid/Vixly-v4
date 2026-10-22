using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: FixtureParcel.cs
//
// PURPOSE:
// A tenant entity for isolation tests, before Operations has a real one.
//
// RESPONSIBILITIES:
// - Carry a tenant and a label
//
// IMPORTANT NOTES:
// - Tests only; its table is created by the fixture, never by a migration
// =========================================================
public sealed class FixtureParcel : OperationsEntity
{
    private FixtureParcel()
    {
        Label = string.Empty;
    }

    public FixtureParcel(Guid tenantId, string label, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        Label = label;
    }

    public string Label { get; private set; }

    public void Rename(string label, DateTimeOffset utcNow)
    {
        Label = label;
        MarkAsModified(utcNow);
    }
}
