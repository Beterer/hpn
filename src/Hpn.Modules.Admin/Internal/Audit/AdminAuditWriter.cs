using System.Text.Json;
using Hpn.Modules.Admin.Internal.Domain;
using Hpn.Modules.Admin.Internal.Persistence;

namespace Hpn.Modules.Admin.Internal.Audit;

internal sealed class AdminAuditWriter(AdminDbContext dbContext, TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AdminAuditLog> WriteAsync(
        Guid adminUserId,
        string action,
        string targetRef,
        object metadata,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(metadata, JsonOptions);
        var audit = AdminAuditLog.Record(adminUserId, action, targetRef, json, timeProvider.GetUtcNow());
        dbContext.AdminAuditLog.Add(audit);
        await dbContext.SaveChangesAsync(cancellationToken);
        return audit;
    }
}
