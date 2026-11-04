using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Jobs;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Jobs;

// =========================================================
// FILE: PlatformJobRunConfiguration.cs
//
// PURPOSE:
// Maps the run history of Control's background jobs (ADR-0007).
//
// RESPONSIBILITIES:
// - Table JobRuns
//
// IMPORTANT NOTES:
// - Indexed by job and start for the Jobs page
// =========================================================
internal sealed class PlatformJobRunConfiguration : IEntityTypeConfiguration<PlatformJobRun>
{
    public void Configure(EntityTypeBuilder<PlatformJobRun> builder)
    {
        builder.ToTable("JobRuns");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Job).HasMaxLength(PlatformJobRun.JobMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.ErrorCode).HasMaxLength(PlatformJobRun.ErrorCodeMaxLength).IsUnicode(false);
        builder.HasIndex(item => new { item.Job, item.CreatedUtc });
        builder.HasIndex(item => item.CreatedUtc);
    }
}
