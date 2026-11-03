using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Printing.PairOperationsPrintAgent;

// =========================================================
// FILE: PairOperationsPrintAgentUseCase.cs
//
// PURPOSE:
// A phone running Vixly Print joins a warehouse with its one-time code and receives its print program key (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous: the code is the proof; rate-limited per address
// - The key is returned once; only its hash is kept
// =========================================================
public sealed class PairOperationsPrintAgentUseCase
{
    private readonly IOperationsPrintingRepository _printing;

    private readonly OperationsPrintAgentKeys _keys;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly IOperationsLocationRepository _locations;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PairOperationsPrintAgentUseCase(
        IOperationsPrintingRepository printing,
        OperationsPrintAgentKeys keys,
        IOperationsSecretGenerator secrets,
        IOperationsLocationRepository locations,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PairOperationsPrintAgentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        PairOperationsPrintAgentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        if (OperationsPrintPairingCode.Tidy(request.Code) is not { } code
            || await _printing.FindPairingAsync(_secrets.HashToken(code), cancellationToken) is not { } pairing
            || !pairing.IsUsableAt(utcNow)
            || await _locations.GetWarehouseAsync(pairing.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return PairOperationsPrintAgentResponse.Failure(OperationsPrintingErrors.PairingInvalid);
        }

        string device = new string((request.DeviceName ?? string.Empty).Where(character => !char.IsControl(character) && character is not '<' and not '>').ToArray()).Trim();
        string name = device.Length == 0 ? pairing.Name : (pairing.Name + " · " + device) is { Length: > OperationsPrintAgent.NameMaxLength } longer ? longer[..OperationsPrintAgent.NameMaxLength] : pairing.Name + " · " + device;
        OperationsSecret secret = _keys.NewSecret();
        OperationsPrintAgent agent = OperationsPrintAgent.RegisterPhone(context.RequiredTenantId, warehouse.Id, name, secret.Hash, utcNow);
        pairing.Use(agent.Id, utcNow);
        _printing.Add(agent);
        _audit.Record(pairing.CreatedByUserId, "ops.printers.phone_paired", nameof(OperationsPrintAgent), agent.Id, after: new { warehouse = warehouse.Code, name = agent.Name });

        // Two phones with one code at once: the row version lets one of them in.
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? PairOperationsPrintAgentResponse.Success(agent.Id, OperationsPrintAgentKeys.Format(agent.Id, secret.Value), warehouse.Name)
            : PairOperationsPrintAgentResponse.Failure(OperationsPrintingErrors.PairingInvalid);
    }
}
