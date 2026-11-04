using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.Devices;

// =========================================================
// FILE: OperationsActors.cs
//
// PURPOSE:
// Builds the actor Operations sends to Platform for a signed-in user.
//
// RESPONSIBILITIES:
// - Name the role Platform knows (owner, admin, staff)
// - Add the request details for Platform's audit
//
// IMPORTANT NOTES:
// - Owner → owner, Administrator → admin, anyone else → staff (design section 3)
// =========================================================
public sealed class OperationsActors
{
    private readonly IOperationsRequestMetadata _request;

    public OperationsActors(IOperationsRequestMetadata request)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
    }

    public OperationsPlatformActor For(OperationsAuthorizationContext context, OperationsUser user, bool stepUp = false)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(user);

        return new OperationsPlatformActor(context.RequiredTenantId, user.Id, user.FullName, user.IsOwner ? "owner" : user.IsAdministrator ? "admin" : "staff", stepUp, _request.ClientIp, _request.UserAgent, _request.Country);
    }
}
