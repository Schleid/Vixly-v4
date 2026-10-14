using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformAuthorizationGateTests.cs
//
// PURPOSE:
// Verifies the Platform authorization context and gate.
//
// RESPONSIBILITIES:
// - Prove the gate denies by default and on empty input
// - Prove denial text does not reveal the permission key
// - Prove the context rejects untrusted shapes
//
// IMPORTANT NOTES:
// - Uses a hand-written fake service; no mocking library
// =========================================================
public sealed class PlatformAuthorizationGateTests
{
    private sealed class FakeAuthorizationService : IPlatformAuthorizationService
    {
        public bool Result { get; set; }

        public int Calls { get; private set; }

        public Task<bool> IsAuthorizedAsync(
            PlatformAuthorizationContext context,
            string requiredPermissionKey,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Result);
        }

        public Task<bool> IsAuthorizedForAnyAsync(
            PlatformAuthorizationContext context,
            IReadOnlyCollection<string> requiredPermissionKeys,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }

    private static PlatformAuthorizationContext NewContext()
    {
        return new PlatformAuthorizationContext(
            Guid.NewGuid(),
            PlatformUserAuthorityLevel.Standard);
    }

    [Fact]
    public void Constructor_RejectsNullDependency()
    {
        Assert.Throws<ArgumentNullException>(() => new PlatformAuthorizationGate(null!));
    }

    [Fact]
    public async Task Authorize_AllowsWhenServiceAllows()
    {
        FakeAuthorizationService service = new() { Result = true };
        PlatformAuthorizationGate gate = new(service);

        await gate.AuthorizeAsync(NewContext(), "platform.users.read");

        Assert.Equal(1, service.Calls);
    }

    [Fact]
    public async Task Authorize_ThrowsWhenServiceDenies_WithoutRevealingTheKey()
    {
        PlatformAuthorizationGate gate = new(new FakeAuthorizationService { Result = false });

        UnauthorizedAccessException exception =
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => gate.AuthorizeAsync(NewContext(), "platform.secret.permission"));

        Assert.DoesNotContain("platform.secret.permission", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Authorize_RejectsBlankPermissionKey(string key)
    {
        FakeAuthorizationService service = new() { Result = true };
        PlatformAuthorizationGate gate = new(service);

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => gate.AuthorizeAsync(NewContext(), key));

        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public async Task AuthorizeAny_DeniesAnEmptySet_WithoutAskingTheService()
    {
        FakeAuthorizationService service = new() { Result = true };
        PlatformAuthorizationGate gate = new(service);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => gate.AuthorizeAnyAsync(NewContext(), Array.Empty<string>()));

        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public async Task AuthorizeAny_DeniesASetContainingABlankKey()
    {
        FakeAuthorizationService service = new() { Result = true };
        PlatformAuthorizationGate gate = new(service);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => gate.AuthorizeAnyAsync(NewContext(), new[] { "platform.users.read", " " }));

        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public async Task AuthorizeAny_AllowsWhenServiceAllows()
    {
        PlatformAuthorizationGate gate = new(new FakeAuthorizationService { Result = true });

        await gate.AuthorizeAnyAsync(NewContext(), new[] { "a", "b" });
    }

    [Fact]
    public void Context_RejectsEmptyUserId()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => new PlatformAuthorizationContext(Guid.Empty, PlatformUserAuthorityLevel.Standard));

        Assert.Equal(PlatformSecurityErrors.UserIdRequired, exception.Error);
    }

    [Fact]
    public void Context_RejectsUndefinedAuthorityLevel()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => new PlatformAuthorizationContext(Guid.NewGuid(), (PlatformUserAuthorityLevel)99));

        Assert.Equal(PlatformSecurityErrors.AuthorityLevelInvalid, exception.Error);
    }
}
