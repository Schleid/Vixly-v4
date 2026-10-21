using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Application.UseCases.SignIn.RequestOperationsPasswordReset;
using Vixly.Operations.Application.UseCases.SignIn.ResetOperationsPassword;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsPasswordResetUseCaseTests.cs
//
// PURPOSE:
// Proves "forgot your password" and the step-up check of Slice 1C.
//
// RESPONSIBILITIES:
// - One email for a real active account, none otherwise, the same answer
// - The link works once, ends every session, keeps the second factor
// - A step-up code works once
//
// IMPORTANT NOTES:
// - In-memory fakes
// =========================================================
public sealed class OperationsPasswordResetUseCaseTests
{
    private readonly IdentityTestKit _kit = new();

    [Fact]
    public async Task Reset_EmailsOnlyARealAccount_AndTheLinkWorksOnce()
    {
        OperationsUser owner = _kit.Owner(withApp: true);
        _kit.Store.Sessions.Add(OperationsUserSession.Start(_kit.TenantId, owner.Id, new string('b', 64), null, null, false, _kit.Clock.GetUtcNow()));

        Assert.True((await Request("nobody@northwind.example")).Succeeded);
        Assert.True((await Request("not an email")).Succeeded);
        Assert.Empty(_kit.Services.ResetMails);

        Assert.True((await Request("GRACE@northwind.example")).Succeeded);
        OperationsPasswordResetMail mail = Assert.Single(_kit.Services.ResetMails);
        Assert.Equal(60, mail.Minutes);
        string token = Uri.UnescapeDataString(mail.Link.Split("token=")[1]);

        ResetOperationsPasswordUseCase reset = new(_kit.Store, _kit.Store, _kit.Services, _kit.Services, _kit.Store, _kit.Audit, _kit.Store, _kit.Clock);
        Assert.Equal("ops.auth.password_too_short", (await reset.ExecuteAsync(_kit.Context, new ResetOperationsPasswordRequest { Token = token, Password = "short" }, CancellationToken.None)).Error?.Code);
        Assert.True((await reset.ExecuteAsync(_kit.Context, new ResetOperationsPasswordRequest { Token = token, Password = "a brand new long password" }, CancellationToken.None)).Succeeded);

        Assert.All(_kit.Store.Sessions, session => Assert.Equal(OperationsSessionEndReason.PasswordChanged, session.EndReason));
        Assert.True(owner.HasSecondFactor);
        Assert.Equal("ops.auth.link_invalid", (await reset.ExecuteAsync(_kit.Context, new ResetOperationsPasswordRequest { Token = token, Password = "another long password" }, CancellationToken.None)).Error?.Code);
        Assert.Contains(_kit.Store.Audit, entry => entry.ActionCode == "ops.auth.password_reset");
    }

    [Fact]
    public void StepUpCode_WorksOnce()
    {
        OperationsUser owner = _kit.Owner(withApp: true);
        OperationsStepUp stepUp = new(_kit.Services, _kit.Clock);

        Assert.Equal("ops.devices.code_required", stepUp.Check(owner, " ")?.Code);
        Assert.Null(stepUp.Check(owner, FakeIdentityServices.GoodCode));
        Assert.Equal("ops.auth.code_invalid", stepUp.Check(owner, FakeIdentityServices.GoodCode)?.Code);
    }

    private Task<RequestOperationsPasswordResetResponse> Request(string email) =>
        new RequestOperationsPasswordResetUseCase(_kit.Store, _kit.Store, _kit.Services, _kit.Store, _kit.Services, _kit.Services, _kit.Audit, _kit.Store, _kit.Clock)
            .ExecuteAsync(_kit.Context, new RequestOperationsPasswordResetRequest { Email = email, PortalSlug = "northwind" }, CancellationToken.None);
}
