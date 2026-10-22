using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Jobs;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsJobRunConfiguration.cs
//
// PURPOSE:
// Table ops.JobRuns.
//
// RESPONSIBILITIES:
// - Map job runs
//
// IMPORTANT NOTES:
// - Indexed by job and start for the latest-run checks
// =========================================================
internal sealed class OperationsJobRunConfiguration : IEntityTypeConfiguration<OperationsJobRun>
{
    public void Configure(EntityTypeBuilder<OperationsJobRun> builder)
    {
        builder.ToTable("JobRuns");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Job).HasMaxLength(OperationsJobRun.JobMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.ErrorCode).HasMaxLength(OperationsJobRun.ErrorCodeMaxLength).IsUnicode(false);
        builder.HasIndex(item => new { item.Job, item.CreatedUtc });
    }
}
