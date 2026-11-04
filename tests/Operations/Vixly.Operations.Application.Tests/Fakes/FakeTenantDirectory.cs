using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeTenantDirectory.cs
//
// PURPOSE:
// A directory with no cache, over the fake Platform.
//
// RESPONSIBILITIES:
// - Pass lookups through and record forgotten tenants
//
// IMPORTANT NOTES:
// - Tests only
// =========================================================
internal sealed class FakeTenantDirectory : IOperationsTenantDirectory
{
    private readonly IOperationsPlatformClient _platform;

    public FakeTenantDirectory(IOperationsPlatformClient platform)
    {
        _platform = platform;
    }

    public List<Guid> Forgotten { get; } = new();

    public Task<OperationsPortalAnswer> FindAsync(string slug, CancellationToken cancellationToken) => _platform.GetPortalAsync(slug, cancellationToken);

    public void Forget(Guid tenantId) => Forgotten.Add(tenantId);
}
