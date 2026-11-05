using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Infrastructure.Persistence;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsParcelHttpTests.cs
//
// PURPOSE:
// Proves roles given for one branch or warehouse (owner, 2026-11-04: "I cannot assign a branch manager to Les Cayes").
//
// RESPONSIBILITIES:
// - The Owner invites a person, and gives an existing person a role, for one branch
// - The Administrator role for a branch is refused with a message that says why
// - Neither form chooses a role in advance
//
// IMPORTANT NOTES:
// - The branch is created in the database, as in the other parcel tests
// =========================================================
public sealed partial class OperationsParcelHttpTests
{
    [SqlServerFact]
    public async Task Owner_GivesARoleForOneBranch_AndAdministratorIsForTheWholeCompany()
    {
        Guid tenant = NewTenant("cayesco", "Cayes Co");
        using CookieClient owner = await OwnerAsync("cayesco");
        Guid branchId = await BranchAsync(tenant);

        (HttpResponseMessage _, string people) = await owner.GetAsync("/staff/people");
        Assert.Matches(ChooseARole, people);
        Assert.Contains("value=\"branch:" + branchId + "\">Branch: Les Cayes</option>", people, StringComparison.Ordinal);
        string manager = Regex.Match(people, "<option value=\"(?<id>[0-9a-f-]{36})\">Branch manager</option>").Groups["id"].Value;
        string administrator = Regex.Match(people, "<option value=\"(?<id>[0-9a-f-]{36})\">Administrator</option>").Groups["id"].Value;

        // Invited as Branch manager of Les Cayes.
        (HttpResponseMessage invited, string _) = await owner.PostAsync("/staff/people/invite", people,
            ("fullName", "Henry Louis"), ("email", "henry@cayesco.example"), ("language", "fr"), ("roleId", manager), ("scope", "branch:" + branchId));
        Assert.Contains("?done=invited", invited.Headers.Location?.OriginalString, StringComparison.Ordinal);
        string member = invited.Headers.Location!.OriginalString.Split('?')[0];
        (HttpResponseMessage _, string henry) = await owner.GetAsync(member);
        Assert.Contains("Les Cayes", henry, StringComparison.Ordinal);
        Assert.Matches(ChooseARole, henry);

        // Administrator for one branch: refused, saying why.
        (HttpResponseMessage refusedInvite, string inviteBody) = await owner.PostAsync("/staff/people/invite", people,
            ("fullName", "Ada Lovelace"), ("email", "ada@cayesco.example"), ("language", "en"), ("roleId", administrator), ("scope", "branch:" + branchId));
        Assert.Equal(HttpStatusCode.OK, refusedInvite.StatusCode);
        Assert.Contains("The Administrator role is for the whole company.", inviteBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Choose a branch or warehouse you are allowed to manage.", inviteBody, StringComparison.Ordinal);

        // An existing person given a role for the branch, from their page; Administrator there is refused the same way.
        (HttpResponseMessage other, string _) = await owner.PostAsync("/staff/people/invite", people,
            ("fullName", "Schwartz Guillaume"), ("email", "schwartz@cayesco.example"), ("language", "fr"), ("roleId", manager), ("scope", "tenant"));
        string schwartz = other.Headers.Location!.OriginalString.Split('?')[0];
        (HttpResponseMessage _, string page) = await owner.GetAsync(schwartz);
        (HttpResponseMessage granted, string _) = await owner.PostAsync(schwartz + "/grant", page, ("roleId", manager), ("scope", "branch:" + branchId));
        Assert.Equal(schwartz + "?done=granted", granted.Headers.Location?.OriginalString);
        (HttpResponseMessage _, string again) = await owner.GetAsync(schwartz);
        (HttpResponseMessage _, string refusedGrant) = await owner.PostAsync(schwartz + "/grant", again, ("roleId", administrator), ("scope", "branch:" + branchId));
        Assert.Contains("The Administrator role is for the whole company.", refusedGrant, StringComparison.Ordinal);

        // A branch that is not the company's is still refused as before.
        (HttpResponseMessage _, string unknown) = await owner.PostAsync(schwartz + "/grant", again, ("roleId", manager), ("scope", "branch:" + Guid.NewGuid()));
        Assert.Contains("Choose a branch or warehouse you are allowed to manage.", unknown, StringComparison.Ordinal);
    }

    // The empty first option, disabled and selected (the renderer writes an empty value as a bare attribute).
    private static readonly Regex ChooseARole = new("<option value(?:=\"\")? disabled selected>Choose a role</option>");

    private async Task<Guid> BranchAsync(Guid tenant)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
        OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OperationsLocationDetails details = new("HT-CAY", "Les Cayes", OperationsPostalAddress.Create("12 Rue Gabions", null, "Les Cayes", "Sud", null, "HT"), null, null, null, true);
        OperationsBranch branch = OperationsBranch.Create(tenant, details, "HT", OperationsBranchLegal.None, now);
        context.Add(branch);
        await context.SaveChangesAsync();
        return branch.Id;
    }
}
