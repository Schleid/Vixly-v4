namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerParcelDetailDto.cs
//
// PURPOSE:
// One package with its history and photos (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerParcelDetailDto(CustomerParcelDto Parcel, IReadOnlyList<CustomerTrackingLineDto> History, IReadOnlyList<CustomerParcelPhotoDto> Photos, string? ReceiptNumber, string WeightUnit, string DimensionUnit,
    string? ActionNeeded = null, decimal? Fee = null, string? FeeCurrency = null,
    IReadOnlyList<string>? Answerable = null, IReadOnlyList<CustomerHoldAnswerDto>? Answers = null,
    decimal StorageFee = 0m,
    string? Currency = null);
