using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Departments;
using Vixly.Platform.Application.UseCases.Departments.AddPlatformDepartmentMember;
using Vixly.Platform.Application.UseCases.Departments.CreatePlatformDepartment;
using Vixly.Platform.Application.UseCases.Departments.DeactivatePlatformDepartment;
using Vixly.Platform.Application.UseCases.Departments.GetPlatformDepartment;
using Vixly.Platform.Application.UseCases.Departments.GetPlatformUserDepartments;
using Vixly.Platform.Application.UseCases.Departments.RemovePlatformDepartmentMember;
using Vixly.Platform.Application.UseCases.Departments.SetPrimaryPlatformDepartment;
using Vixly.Platform.Application.UseCases.Departments.UpdatePlatformDepartment;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformDepartmentAdministrationTests.cs
//
// PURPOSE:
// Proves the department administration use cases.
//
// RESPONSIBILITIES:
// - Create and update: unique names, audit before and after
// - Members: add, primary, remove; inactive departments and
//   higher-authority targets refused
// - What the screens offer: actions and joinable departments
//
// IMPORTANT NOTES:
// - Uses the real gate and authorization service over the
//   in-memory store
// =========================================================
public sealed class PlatformDepartmentAdministrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly FakePlatformStore _store = new();

    private readonly FixedTimeProvider _clock = new(Now);

    [Fact]
    public async Task CreateDepartment_RefusesATakenName_AndAuditsTheNewOne()
    {
        PlatformAuthorizationContext context = SuperUser();
        Department("Billing");

        CreatePlatformDepartmentResponse taken = await Create().ExecuteAsync(context, new CreatePlatformDepartmentRequest { Name = "BILLING" }, CancellationToken.None);
        CreatePlatformDepartmentResponse created = await Create().ExecuteAsync(
            context,
            new CreatePlatformDepartmentRequest { Name = " Support ", Description = "  First line  " },
            CancellationToken.None);

        Assert.Equal(PlatformDepartmentErrors.NameAlreadyUsed, taken.Error);
        PlatformDepartment support = _store.Departments.Single(department => department.Id == created.PlatformDepartmentId);
        Assert.Equal(("Support", "First line"), (support.Name, support.Description));
        Assert.Equal("platform.departments.created", _store.AuditEntries.Single().ActionCode);
    }

    [Fact]
    public async Task UpdateDepartment_RefusesAnotherDepartmentsName_ButNotItsOwn()
    {
        PlatformAuthorizationContext context = SuperUser();
        Department("Billing");
        PlatformDepartment support = Department("Support");

        UpdatePlatformDepartmentResponse clash = await Update().ExecuteAsync(
            context,
            new UpdatePlatformDepartmentRequest { PlatformDepartmentId = support.Id, Name = "billing" },
            CancellationToken.None);
        UpdatePlatformDepartmentResponse described = await Update().ExecuteAsync(
            context,
            new UpdatePlatformDepartmentRequest { PlatformDepartmentId = support.Id, Name = "Support", Description = "Phones" },
            CancellationToken.None);

        Assert.Equal(PlatformDepartmentErrors.NameAlreadyUsed, clash.Error);
        Assert.True(described.Succeeded);
        Assert.Equal("Phones", support.Description);
    }

    [Fact]
    public async Task Members_AddPrimaryAndRemove_AreAudited()
    {
        PlatformAuthorizationContext context = SuperUser();
        PlatformUser ada = NewUser(PlatformUserAuthorityLevel.Standard);
        PlatformDepartment billing = Department("Billing");
        PlatformDepartment support = Department("Support");

        Assert.True((await AddAsync(context, ada, billing)).Succeeded);
        Assert.True((await AddAsync(context, ada, support)).Succeeded);
        Assert.Equal(billing.Id, ada.Departments.Single(membership => membership.IsPrimary).PlatformDepartmentId);

        Assert.True((await SetPrimary().ExecuteAsync(context, new SetPrimaryPlatformDepartmentRequest { PlatformUserId = ada.Id, PlatformDepartmentId = support.Id }, CancellationToken.None)).Succeeded);
        Assert.Equal(support.Id, ada.Departments.Single(membership => membership.IsPrimary).PlatformDepartmentId);

        Assert.True((await Remove().ExecuteAsync(context, new RemovePlatformDepartmentMemberRequest { PlatformUserId = ada.Id, PlatformDepartmentId = support.Id }, CancellationToken.None)).Succeeded);
        Assert.True(ada.Departments.Single().IsPrimary);
        Assert.Equal(
            new[] { "platform.departments.member_added", "platform.departments.member_added", "platform.departments.primary_set", "platform.departments.member_removed" },
            _store.AuditEntries.Select(entry => entry.ActionCode));
    }

    [Fact]
    public async Task AddMember_RefusesInactiveDepartments_AndHigherAuthorityTargets()
    {
        (PlatformUser actor, PlatformAuthorizationContext context) = Standard(PlatformPermissions.DepartmentsManageMembers, PlatformPermissions.DepartmentsDeactivate);
        PlatformUser target = NewUser(PlatformUserAuthorityLevel.Standard);
        PlatformUser superUser = NewUser(PlatformUserAuthorityLevel.SuperUser);
        PlatformDepartment billing = Department("Billing");
        await Deactivate().ExecuteAsync(context, new DeactivatePlatformDepartmentRequest { PlatformDepartmentId = billing.Id }, CancellationToken.None);
        PlatformDepartment support = Department("Support");

        Assert.Equal(PlatformDepartmentErrors.DepartmentInactive, (await AddAsync(context, target, billing)).Error);
        Assert.Equal(PlatformAuthErrors.TargetAuthorityHigher, (await AddAsync(context, superUser, support)).Error);
        Assert.True((await AddAsync(context, actor, support)).Succeeded);
    }

    [Fact]
    public async Task GetDepartment_OffersOnlyWhatTheActorMayDo()
    {
        (_, PlatformAuthorizationContext context) = Standard(PlatformPermissions.DepartmentsView, PlatformPermissions.DepartmentsEdit);
        PlatformDepartment billing = Department("Billing");

        GetPlatformDepartmentResponse response = await Get().ExecuteAsync(context, new GetPlatformDepartmentRequest { PlatformDepartmentId = billing.Id }, CancellationToken.None);

        Assert.Equal(new PlatformDepartmentActions(true, false, false, false), response.Actions);
    }

    [Fact]
    public async Task GetUserDepartments_OffersActiveDepartmentsNotJoinedYet()
    {
        PlatformAuthorizationContext context = SuperUser();
        PlatformUser ada = NewUser(PlatformUserAuthorityLevel.Standard);
        PlatformDepartment billing = Department("Billing");
        PlatformDepartment support = Department("Support");
        Department("Old").Deactivate(Now);
        ada.JoinDepartment(billing.Id, Now);

        GetPlatformUserDepartmentsResponse response = await UserDepartments().ExecuteAsync(
            context,
            new GetPlatformUserDepartmentsRequest { PlatformUserId = ada.Id },
            CancellationToken.None);

        Assert.True(response.CanManage);
        Assert.Equal(billing.Id, response.Memberships.Single().PlatformDepartmentId);
        Assert.Equal(new[] { support.Id }, response.Joinable.Select(department => department.PlatformDepartmentId));
    }

    private Task<AddPlatformDepartmentMemberResponse> AddAsync(PlatformAuthorizationContext context, PlatformUser user, PlatformDepartment department)
    {
        return Add().ExecuteAsync(
            context,
            new AddPlatformDepartmentMemberRequest { PlatformUserId = user.Id, PlatformDepartmentId = department.Id },
            CancellationToken.None);
    }

    private CreatePlatformDepartmentUseCase Create()
    {
        return new CreatePlatformDepartmentUseCase(Gate(), _store, Audit(), _store, _clock);
    }

    private UpdatePlatformDepartmentUseCase Update()
    {
        return new UpdatePlatformDepartmentUseCase(Departments(), _store, Audit(), _store, _clock);
    }

    private DeactivatePlatformDepartmentUseCase Deactivate()
    {
        return new DeactivatePlatformDepartmentUseCase(Departments(), Audit(), _store, _clock);
    }

    private AddPlatformDepartmentMemberUseCase Add()
    {
        return new AddPlatformDepartmentMemberUseCase(Users(), _store, Audit(), _store, _clock);
    }

    private RemovePlatformDepartmentMemberUseCase Remove()
    {
        return new RemovePlatformDepartmentMemberUseCase(Users(), Audit(), _store, _clock);
    }

    private SetPrimaryPlatformDepartmentUseCase SetPrimary()
    {
        return new SetPrimaryPlatformDepartmentUseCase(Users(), Audit(), _store, _clock);
    }

    private GetPlatformDepartmentUseCase Get()
    {
        return new GetPlatformDepartmentUseCase(Gate(), _store, Departments());
    }

    private GetPlatformUserDepartmentsUseCase UserDepartments()
    {
        return new GetPlatformUserDepartmentsUseCase(Gate(), _store, _store, Departments(), _clock);
    }

    private PlatformDepartmentAdministration Departments()
    {
        return new PlatformDepartmentAdministration(Gate(), _store, _store, _clock);
    }

    private PlatformUserAdministration Users()
    {
        return new PlatformUserAdministration(Gate(), new PlatformStepUpGuard(_store, _clock), _store, _store, _clock);
    }

    private PlatformAuthorizationGate Gate()
    {
        return new PlatformAuthorizationGate(new PlatformAuthorizationService(_store, _clock));
    }

    private PlatformAuditTrail Audit()
    {
        return new PlatformAuditTrail(_store, _store, _clock);
    }

    private PlatformDepartment Department(string name)
    {
        PlatformDepartment department = PlatformDepartment.Create(name, null, Now);
        _store.Departments.Add(department);

        return department;
    }

    private PlatformAuthorizationContext SuperUser()
    {
        PlatformUser user = NewUser(PlatformUserAuthorityLevel.SuperUser);
        user.AcceptInvitation("H:password", Now);

        return new PlatformAuthorizationContext(user.Id, user.AuthorityLevel);
    }

    private (PlatformUser User, PlatformAuthorizationContext Context) Standard(params string[] codes)
    {
        PlatformUser user = NewUser(PlatformUserAuthorityLevel.Standard);
        user.AcceptInvitation("H:password", Now);
        PlatformRole role = PlatformRole.CreateCustom("Role " + _store.Roles.Count, Now);
        role.ReplacePermissions(codes, Now);
        _store.Roles.Add(role);
        user.AssignRole(role.Id, Guid.NewGuid(), null, Now);

        return (user, new PlatformAuthorizationContext(user.Id, user.AuthorityLevel));
    }

    private PlatformUser NewUser(PlatformUserAuthorityLevel level)
    {
        PlatformUser user = PlatformUser.Invite(
            "Test",
            "User" + _store.Users.Count,
            EmailAddress.Create($"d{_store.Users.Count}@example.com"),
            LanguageCode.En,
            level,
            null,
            null,
            Now);
        _store.Users.Add(user);

        return user;
    }
}
