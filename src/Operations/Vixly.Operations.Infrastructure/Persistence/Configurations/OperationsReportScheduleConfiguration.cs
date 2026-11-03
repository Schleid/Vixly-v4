using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Reports;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsReportScheduleConfiguration.cs
//
// PURPOSE:
// Maps the scheduled exports (Slice 11).
//
// RESPONSIBILITIES:
// - Columns and indexes; recipients as JSON
//
// IMPORTANT NOTES:
// - The due ones are found by their next run
// =========================================================
internal sealed class OperationsReportScheduleConfiguration : IEntityTypeConfiguration<OperationsReportSchedule>
{
    public void Configure(EntityTypeBuilder<OperationsReportSchedule> builder)
    {
        builder.ToTable("ReportSchedules");
        builder.HasKey(schedule => schedule.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(schedule => schedule.ReportKey).HasMaxLength(OperationsReportSchedule.KeyMaxLength).IsUnicode(false).IsRequired();
        builder.PrimitiveCollection(schedule => schedule.Recipients).HasField("_recipients").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(schedule => schedule.NextRunUtc);
        builder.HasIndex(schedule => schedule.TenantId);
    }
}
