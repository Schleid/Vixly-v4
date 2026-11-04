using Vixly.Operations.Application.UseCases.Settings.AdoptOperationsCompanyTimeZone;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: AdoptOperationsCompanyTimeZoneUseCaseTests.cs
//
// PURPOSE:
// Proves Operations copies the company's time zone once (owner, 2026-11-02).
//
// RESPONSIBILITIES:
// - Empty Operations settings take the company's zone
// - A zone already chosen in Operations is kept
// - UTC or an unknown zone from Platform changes nothing
//
// IMPORTANT NOTES:
// - Each test uses its own tenant id: the use case remembers checked tenants per process
// =========================================================
public sealed class AdoptOperationsCompanyTimeZoneUseCaseTests
{
    private readonly IdentityTestKit _kit = new();

    private AdoptOperationsCompanyTimeZoneUseCase UseCase() => new(_kit.Store, _kit.Audit, _kit.Store, _kit.Clock);

    private async Task<bool> CopyAsync(string? zone) =>
        (await UseCase().ExecuteAsync(_kit.Context, new AdoptOperationsCompanyTimeZoneRequest { TenantId = _kit.TenantId, CompanyTimeZone = zone }, CancellationToken.None)).Copied;

    [Fact]
    public async Task NoSettingsYet_TakeTheCompanyZone_Once()
    {
        Assert.True(await CopyAsync("America/New_York"));

        Assert.Equal("America/New_York", _kit.Store.Settings!.TimeZone);
        Assert.Contains(_kit.Store.Audit, entry => entry.ActionCode == "ops.settings.time_zone_from_company");
        Assert.True(AdoptOperationsCompanyTimeZoneUseCase.AlreadyChecked(_kit.TenantId));
        Assert.False(await CopyAsync("America/Chicago"));
        Assert.Equal("America/New_York", _kit.Store.Settings.TimeZone);
    }

    [Fact]
    public async Task AZoneChosenInOperations_IsKept()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(_kit.TenantId, _kit.Clock.GetUtcNow());
        settings.ChangeTimeZone("America/Port-au-Prince", _kit.Clock.GetUtcNow());
        _kit.Store.Settings = settings;

        Assert.False(await CopyAsync("America/New_York"));
        Assert.Equal("America/Port-au-Prince", _kit.Store.Settings.TimeZone);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("UTC")]
    [InlineData("Not/AZone")]
    public async Task UtcOrUnknown_ChangesNothing(string? zone)
    {
        Assert.False(await CopyAsync(zone));
        Assert.Null(_kit.Store.Settings?.TimeZone);
    }
}
