using Vixly.Operations.Domain.Texts;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsTextRepository.cs
//
// PURPOSE:
// The company's Twilio account and the log of text messages sent (Slice 9).
//
// RESPONSIBILITIES:
// - Find, add and remove the account; add and list log rows
//
// IMPORTANT NOTES:
// - Tenant-filtered like every Operations read
// =========================================================
public interface IOperationsTextRepository
{
    Task<OperationsTextProvider?> GetProviderAsync(CancellationToken cancellationToken);

    void Add(OperationsTextProvider provider);

    void Remove(OperationsTextProvider provider);

    void Add(OperationsTextMessage message);

    Task<OperationsTextMessage?> FindMessageAsync(string providerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsTextMessage>> ListMessagesAsync(int take, CancellationToken cancellationToken);
}
