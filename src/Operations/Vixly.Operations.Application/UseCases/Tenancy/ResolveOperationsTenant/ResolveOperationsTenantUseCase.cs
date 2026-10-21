using System.Text.RegularExpressions;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tenancy;
using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Application.UseCases.Tenancy.ResolveOperationsTenant;

// =========================================================
// FILE: ResolveOperationsTenantUseCase.cs
//
// PURPOSE:
// Tells which tenant a host name belongs to and what it may do there (Stage 3, Slice 0, D-F2).
//
// RESPONSIBILITIES:
// - Check the slug shape before asking Platform
// - Map Platform's answer to live, setting up, suspended or not found
//
// IMPORTANT NOTES:
// - Runs with the Host context: no tenant is known yet
// - Unknown, deactivated and banned tenants all read "not found" (Platform answers alike)
// - Platform unreachable is an error, never "not found", so a live tenant is not turned away by mistake
// =========================================================
public sealed class ResolveOperationsTenantUseCase
{
    private static readonly Regex SlugShape = new("^[a-z0-9](?:[a-z0-9-]{1,61}[a-z0-9])?$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly IOperationsTenantDirectory _directory;

    public ResolveOperationsTenantUseCase(IOperationsTenantDirectory directory)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
    }

    public async Task<ResolveOperationsTenantResponse> ExecuteAsync(
        OperationsAuthorizationContext authorizationContext,
        ResolveOperationsTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        // =====================================================
        // TRUSTED CONTEXT VALIDATION
        // =====================================================
        ArgumentNullException.ThrowIfNull(authorizationContext);
        ArgumentNullException.ThrowIfNull(request);

        // =====================================================
        // REQUEST VALIDATION
        // =====================================================
        string slug = request.Slug?.Trim().ToLowerInvariant() ?? string.Empty;

        if (!SlugShape.IsMatch(slug))
        {
            return ResolveOperationsTenantResponse.Success(OperationsTenantHost.NotFound);
        }

        // =====================================================
        // PLATFORM LOOKUP
        // =====================================================
        OperationsPortalAnswer answer;

        try
        {
            answer = await _directory.FindAsync(slug, cancellationToken);
        }
        catch (OperationsPlatformUnavailableException)
        {
            return ResolveOperationsTenantResponse.Failure(OperationsCommonErrors.PlatformUnavailable);
        }

        return ResolveOperationsTenantResponse.Success(Map(answer));
    }

    // =====================================================
    // METHOD: MAP PLATFORM ANSWER
    // =====================================================
    private static OperationsTenantHost Map(OperationsPortalAnswer answer)
    {
        if (!answer.Found || answer.TenantId is not { } tenantId)
        {
            return OperationsTenantHost.NotFound;
        }

        OperationsTenantState state = answer.Status switch
        {
            "active" => OperationsTenantState.Live,
            "suspended" => OperationsTenantState.Suspended,
            "draft" or "pending_activation" => OperationsTenantState.SettingUp,
            _ => OperationsTenantState.NotFound
        };

        if (state == OperationsTenantState.NotFound)
        {
            return OperationsTenantHost.NotFound;
        }

        OperationsTenantAccess access = answer.Access switch
        {
            "full" => OperationsTenantAccess.Full,
            "read_only" => OperationsTenantAccess.ReadOnly,
            "payment_required" => OperationsTenantAccess.PaymentRequired,
            _ => OperationsTenantAccess.None
        };

        return new OperationsTenantHost(state, tenantId, access, answer.CanonicalSlug, answer.DefaultLanguage, string.IsNullOrWhiteSpace(answer.Legal) ? "not_required" : answer.Legal);
    }
}
