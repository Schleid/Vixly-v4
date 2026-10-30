using System.Text.Json;
using System.Text.RegularExpressions;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsTrackingEvent.cs
//
// PURPOSE:
// One append-only tracking event (ADR-0021).
//
// RESPONSIBILITIES:
// - Check the code, its arguments, the time and the idempotency key
// - Keep the visibility of its code
//
// IMPORTANT NOTES:
// - Never edited: a wrong event is voided by a new event (decision 2)
// - The app login has INSERT and SELECT only on the table (global SQL script)
// - Recorded time is CreatedUtc; occurred time may be earlier (offline scans)
// =========================================================
public sealed class OperationsTrackingEvent : OperationsEntity
{
    public const int CodeMaxLength = 100;

    public const int ArgumentValueMaxLength = 200;

    public const int ArgumentsMaxLength = 2000;

    public const int TimeZoneMaxLength = 64;

    public const int LocationCodeMaxLength = 32;

    public const int IdempotencyKeyMaxLength = 100;

    public const int NoteMaxLength = 500;

    // Scans from a device with a slightly fast clock are accepted; anything later is refused.
    public static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(5);

    private static readonly Regex LocationPattern = new(@"^[A-Za-z0-9._\-]{1,32}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex KeyPattern = new(@"^[A-Za-z0-9:_.\-]{8,100}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly DateTimeOffset Earliest = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private OperationsTrackingEvent()
    {
        Code = string.Empty;
        IdempotencyKey = string.Empty;
    }

    private OperationsTrackingEvent(Guid tenantId, DateTimeOffset recordedUtc)
        : base(tenantId, recordedUtc)
    {
        Code = string.Empty;
        IdempotencyKey = string.Empty;
    }

    public OperationsTrackingSubject SubjectKind { get; private set; }

    public Guid SubjectId { get; private set; }

    public string Code { get; private set; }

    public string? ArgumentsJson { get; private set; }

    public DateTimeOffset OccurredUtc { get; private set; }

    public string? TimeZone { get; private set; }

    public string? LocationCode { get; private set; }

    public OperationsTrackingActor Actor { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public Guid? DeviceId { get; private set; }

    public OperationsTrackingSource Source { get; private set; }

    public OperationsTrackingVisibility Visibility { get; private set; }

    public string IdempotencyKey { get; private set; }

    public Guid? VoidsEventId { get; private set; }

    public string? Note { get; private set; }

    // Slice 6C (E-4): the event on a shipment, bundle or dispatch this parcel's copy comes from.
    public Guid? CausedByEventId { get; private set; }

    public static OperationsTrackingEvent Record(Guid tenantId, OperationsTrackingDraft draft, DateTimeOffset recordedUtc)
    {
        ArgumentNullException.ThrowIfNull(draft);

        OperationsTrackingCode code = OperationsTrackingCodes.Find(draft.Code) ?? throw new DomainException(OperationsTrackingErrors.CodeUnknown);

        if (draft.SubjectId == Guid.Empty || !Enum.IsDefined(draft.SubjectKind))
        {
            throw new DomainException(OperationsTrackingErrors.SubjectRequired);
        }

        if (draft.IdempotencyKey is null || !KeyPattern.IsMatch(draft.IdempotencyKey))
        {
            throw new DomainException(OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        if (draft.OccurredUtc < Earliest || draft.OccurredUtc > recordedUtc.Add(ClockTolerance) || !IsTimeZone(draft.TimeZone))
        {
            throw new DomainException(OperationsTrackingErrors.TimeInvalid);
        }

        if (draft.LocationCode is not null && !LocationPattern.IsMatch(draft.LocationCode))
        {
            throw new DomainException(OperationsTrackingErrors.LocationInvalid);
        }

        // A staff actor names the person (ADR-0021: who); unknown kinds are refused, never guessed.
        if (!Enum.IsDefined(draft.Actor) || !Enum.IsDefined(draft.Source) || (draft.Actor == OperationsTrackingActor.Staff && (draft.ActorUserId is null || draft.ActorUserId == Guid.Empty)))
        {
            throw new DomainException(OperationsTrackingErrors.ActorInvalid);
        }

        bool namesTarget = draft.VoidsEventId is { } target && target != Guid.Empty;

        if (code.Voids != namesTarget)
        {
            throw new DomainException(OperationsTrackingErrors.VoidTargetRequired);
        }

        return new OperationsTrackingEvent(tenantId, recordedUtc)
        {
            SubjectKind = draft.SubjectKind,
            SubjectId = draft.SubjectId,
            Code = code.Name,
            ArgumentsJson = CheckArguments(code, draft.Arguments),
            OccurredUtc = draft.OccurredUtc.ToUniversalTime(),
            TimeZone = draft.TimeZone,
            LocationCode = draft.LocationCode,
            Actor = draft.Actor,
            ActorUserId = draft.ActorUserId,
            DeviceId = draft.DeviceId,
            Source = draft.Source,
            Visibility = code.Visibility,
            IdempotencyKey = draft.IdempotencyKey,
            VoidsEventId = code.Voids ? draft.VoidsEventId : null,
            Note = Clip(draft.Note, NoteMaxLength),
            CausedByEventId = draft.CausedByEventId
        };
    }

    public IReadOnlyDictionary<string, string> Arguments() =>
        ArgumentsJson is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : JsonSerializer.Deserialize<Dictionary<string, string>>(ArgumentsJson) ?? new Dictionary<string, string>(StringComparer.Ordinal);

    // The hold reason named by a hold event; None for any other event.
    public OperationsHoldReasons HoldReason() =>
        Arguments().TryGetValue(OperationsTrackingCode.ReasonArgument, out string? value) && Enum.TryParse(value, false, out OperationsHoldReasons reason) ? reason : OperationsHoldReasons.None;

    private static string? CheckArguments(OperationsTrackingCode code, IReadOnlyDictionary<string, string>? arguments)
    {
        Dictionary<string, string> given = arguments is null ? new(StringComparer.Ordinal) : new(arguments, StringComparer.Ordinal);

        if (given.Keys.Any(name => !code.Arguments.Contains(name, StringComparer.Ordinal) && !code.OptionalArguments.Contains(name, StringComparer.Ordinal)))
        {
            throw new DomainException(OperationsTrackingErrors.ArgumentUnknown);
        }

        if (code.Arguments.Any(name => !given.ContainsKey(name)))
        {
            throw new DomainException(OperationsTrackingErrors.ArgumentMissing);
        }

        foreach ((string name, string value) in given)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > ArgumentValueMaxLength || value.Any(char.IsControl))
            {
                throw new DomainException(OperationsTrackingErrors.ArgumentInvalid);
            }

            // Controlled arguments take one value from their list (a hold names exactly one ADR-0018 reason).
            if (code.Choices.TryGetValue(name, out IReadOnlyList<string>? allowed) && !allowed.Contains(value, StringComparer.Ordinal))
            {
                throw new DomainException(OperationsTrackingErrors.ArgumentInvalid);
            }
        }

        if (given.Count == 0)
        {
            return null;
        }

        string json = JsonSerializer.Serialize(new SortedDictionary<string, string>(given, StringComparer.Ordinal));
        return json.Length <= ArgumentsMaxLength ? json : throw new DomainException(OperationsTrackingErrors.ArgumentInvalid);
    }

    // An IANA or Windows time zone id the server knows, or none.
    private static bool IsTimeZone(string? id) =>
        id is null || (id.Length <= TimeZoneMaxLength && TimeZoneInfo.TryFindSystemTimeZoneById(id, out _));

    private static string? Clip(string? value, int maxLength)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return text is { Length: > 0 } && text.Length > maxLength ? text[..maxLength] : text;
    }
}
