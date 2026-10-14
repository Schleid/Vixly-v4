using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Web.Security;

namespace Vixly.Platform.Web.Features;

// =========================================================
// FILE: PlatformUseCaseScope.cs
//
// PURPOSE:
// Starts each use case a presenter runs: fresh data, trusted
// identity.
//
// RESPONSIBILITIES:
// - Forget what the previous operation of this screen loaded, so
//   the next use case never reads a stale record
// - Give the signed-in user's authorization context (S-2)
//
// IMPORTANT NOTES:
// - A feature component keeps one DI scope for its whole life
//   (OwningComponentBase); without the reset, a record read once
//   would be served from memory, out of date, for every later
//   operation
// - Operations of one presenter never overlap (busy flag), so
//   the reset never drops a change another operation is making
// =========================================================
public sealed class PlatformUseCaseScope
{
    private readonly PlatformCurrentUser _currentUser;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public PlatformUseCaseScope(PlatformCurrentUser currentUser, IPlatformUnitOfWork unitOfWork)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public PlatformAuthorizationContext Begin()
    {
        _unitOfWork.DiscardChanges();

        return _currentUser.GetContext();
    }
}
