using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Screens;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Screens;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Screens.ConnectOperationsCustomerScreen;

// =========================================================
// FILE: ConnectOperationsCustomerScreenUseCase.cs
//
// PURPOSE:
// The screen itself, with the code a clerk gave: it becomes that customer screen and receives its own secret (Slice 5F step 3, P-2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - No sign-in: the company comes from the address; the code is the proof (one use, 15 minutes, rate limited)
// - The secret goes to the screen's own cookie; only its hash is kept
// =========================================================
public sealed class ConnectOperationsCustomerScreenUseCase
{
    private readonly OperationsScreenDesk _desk;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ConnectOperationsCustomerScreenUseCase(
        OperationsScreenDesk desk,
        IOperationsSecretGenerator secrets,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ConnectOperationsCustomerScreenResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ConnectOperationsCustomerScreenRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        if (context.TenantId is null || string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 20
            || await _desk.Screens.FindByCodeAsync(_secrets.HashRecoveryCode(request.Code), cancellationToken) is not { } screen || !screen.CodeValid(utcNow))
        {
            return ConnectOperationsCustomerScreenResponse.Failure(OperationsScreenErrors.CodeInvalid);
        }

        OperationsSecret secret = _secrets.NewToken();
        screen.Pair(secret.Hash, utcNow);
        _audit.Record(null, "ops.screens.paired", nameof(OperationsCustomerScreen), screen.Id, after: new { screen.Name, warehouse = screen.WarehouseId });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ConnectOperationsCustomerScreenResponse.Success(secret.Value)
            : ConnectOperationsCustomerScreenResponse.Failure(OperationsScreenErrors.CodeInvalid);
    }
}
