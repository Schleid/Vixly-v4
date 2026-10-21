using System.Text.Json;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Audit;

namespace Vixly.Operations.Application.Audit;

// =========================================================
// FILE: OperationsAuditTrail.cs
//
// PURPOSE:
// Writes audit entries for the current tenant (Slice 1, D-2).
//
// RESPONSIBILITIES:
// - Serialize arguments and add one entry with the request facts
//
// IMPORTANT NOTES:
// - Added to the same unit of work as the change
// - The tenant comes from the scope; a system scope has no tenant and cannot audit
// =========================================================
public sealed class OperationsAuditTrail
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOperationsAuditRepository _audit;

    private readonly IOperationsTenantScope _scope;

    private readonly IOperationsRequestMetadata _metadata;

    private readonly TimeProvider _timeProvider;

    public OperationsAuditTrail(IOperationsAuditRepository audit, IOperationsTenantScope scope, IOperationsRequestMetadata metadata, TimeProvider timeProvider)
    {
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public void Record(Guid? actorUserId, string actionCode, string targetType, Guid? targetId, object? arguments = null, object? before = null, object? after = null, string? reason = null)
    {
        Guid tenantId = _scope.TenantId ?? throw new InvalidOperationException("Audit needs a tenant scope.");

        _audit.Add(OperationsAuditEntry.Record(
            tenantId,
            new OperationsAuditRecord
            {
                ActorUserId = actorUserId,
                ActionCode = actionCode,
                TargetType = targetType,
                TargetId = targetId,
                ArgumentsJson = Serialize(arguments),
                BeforeJson = Serialize(before),
                AfterJson = Serialize(after),
                Reason = reason,
                IpAddress = _metadata.ClientIp,
                UserAgent = _metadata.UserAgent,
                CorrelationId = _metadata.CorrelationId
            },
            _timeProvider.GetUtcNow()));
    }

    private static string? Serialize(object? value) => value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
}
