using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Shared.Kernel.Tests;

// =========================================================
// FILE: AuditableEntityTests.cs
//
// PURPOSE:
// Verifies identity and audit-timestamp behavior of the base
// entity.
//
// RESPONSIBILITIES:
// - Cover id and creation-time assignment
// - Cover UTC enforcement and modification tracking
//
// IMPORTANT NOTES:
// - Uses a private test entity; the Kernel owns no real
//   entities (Global Standard 3, rule 6)
// =========================================================
public sealed class AuditableEntityTests
{
    private sealed class TestEntity : AuditableEntity
    {
        public TestEntity(DateTimeOffset createdUtc)
            : base(createdUtc)
        {
        }
    }

    [Fact]
    public void Constructor_AssignsVersion7IdAndCreationTime()
    {
        DateTimeOffset created = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

        TestEntity entity = new(created);

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(7, (entity.Id.ToByteArray(bigEndian: true)[6] >> 4));
        Assert.Equal(created, entity.CreatedUtc);
        Assert.Null(entity.LastModifiedUtc);
    }

    [Fact]
    public void Ids_SortByCreationTime()
    {
        TestEntity earlier = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        TestEntity later = new(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero));

        Assert.True(earlier.Id.CompareTo(later.Id) < 0);
    }

    [Fact]
    public void Constructor_RejectsNonUtcTime()
    {
        DateTimeOffset local = new(2026, 10, 6, 12, 0, 0, TimeSpan.FromHours(2));

        DomainException exception =
            Assert.Throws<DomainException>(() => new TestEntity(local));

        Assert.Equal(KernelErrors.TimeMustBeUtc, exception.Error);
    }

    [Fact]
    public void MarkAsModified_RecordsTimeAndRejectsNonUtc()
    {
        TestEntity entity = new(new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero));
        DateTimeOffset modified = new(2026, 10, 7, 8, 30, 0, TimeSpan.Zero);

        entity.MarkAsModified(modified);

        Assert.Equal(modified, entity.LastModifiedUtc);
        Assert.Throws<DomainException>(
            () => entity.MarkAsModified(new DateTimeOffset(2026, 10, 8, 0, 0, 0, TimeSpan.FromHours(1))));
    }
}
