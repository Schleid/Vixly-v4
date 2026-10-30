using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsSignInChallengeRepository.cs
//
// PURPOSE:
// Pending sign-ins of the current tenant.
//
// RESPONSIBILITIES:
// - Add and find by token hash
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1)
// =========================================================
public interface IOperationsSignInChallengeRepository
{
    void Add(OperationsSignInChallenge challenge);

    Task<OperationsSignInChallenge?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
}
