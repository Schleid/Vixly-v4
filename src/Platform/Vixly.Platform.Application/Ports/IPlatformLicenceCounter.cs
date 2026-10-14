namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformLicenceCounter.cs
//
// PURPOSE:
// Counts a tenant's licences in use.
//
// RESPONSIBILITIES:
// - Say how many licences a tenant has allocated to devices
//
// IMPORTANT NOTES:
// - Licensing arrives in Slice 7; until then the implementation answers zero
// - Used to refuse a downgrade or a decrease below the licences in use (D-S4, D-S5)
// =========================================================
public interface IPlatformLicenceCounter
{
    Task<int> CountActiveAsync(Guid platformTenantId, CancellationToken cancellationToken);
}
