using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Api;

// =========================================================
// FILE: OperationsApiErrors.cs
//
// PURPOSE:
// Error codes of the company API and its webhooks (Slice 10).
//
// RESPONSIBILITIES:
// - Name each refusal
//
// IMPORTANT NOTES:
// - Codes only; texts in OperationsText (en, fr, es)
// =========================================================
public static class OperationsApiErrors
{
    public static Error NameRequired { get; } = Error.Create("ops.api.name_required");

    public static Error TooManyKeys { get; } = Error.Create("ops.api.too_many_keys");

    public static Error KeyNotFound { get; } = Error.Create("ops.api.key_not_found");

    public static Error UrlInvalid { get; } = Error.Create("ops.api.url_invalid");

    public static Error EventsRequired { get; } = Error.Create("ops.api.events_required");

    public static Error TooManyEndpoints { get; } = Error.Create("ops.api.too_many_endpoints");

    public static Error EndpointNotFound { get; } = Error.Create("ops.api.endpoint_not_found");

    public static Error DeliveryNotFound { get; } = Error.Create("ops.api.delivery_not_found");

    public static Error FeatureOff { get; } = Error.Create("ops.api.feature_off");
}
