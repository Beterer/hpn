using Hpn.Modules.Admin.Internal.ReadModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hpn.Modules.Admin.Internal.Persistence.Configurations;

internal sealed class AdminQueueItemReadModelConfiguration : IEntityTypeConfiguration<AdminQueueItemReadModel>
{
    public void Configure(EntityTypeBuilder<AdminQueueItemReadModel> builder)
    {
        builder.HasNoKey();
        builder.ToView(null);
    }
}

internal sealed class AdminReportReadModelConfiguration : IEntityTypeConfiguration<AdminReportReadModel>
{
    public void Configure(EntityTypeBuilder<AdminReportReadModel> builder)
    {
        builder.HasNoKey();
        builder.ToView(null);
    }
}

internal sealed class AdminStatsReadModelConfiguration : IEntityTypeConfiguration<AdminStatsReadModel>
{
    public void Configure(EntityTypeBuilder<AdminStatsReadModel> builder)
    {
        builder.HasNoKey();
        builder.ToView(null);
    }
}
