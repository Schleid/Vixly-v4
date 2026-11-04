using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsPlatformCall.cs
//
// PURPOSE:
// The result of a Platform action: a value, or the refusal translated to an ops code.
//
// RESPONSIBILITIES:
// - Carry the value or the error
//
// IMPORTANT NOTES:
// - Platform being down is an exception (OperationsPlatformUnavailableException), not a refusal
// =========================================================
public sealed record OperationsPlatformCall<T>(T? Value, Error? Error)
{
    public bool Succeeded => Error is null;

    public static OperationsPlatformCall<T> Ok(T value) => new(value, null);

    public static OperationsPlatformCall<T> Refused(Error error) => new(default, error);
}
