namespace tests;

public partial class FluentPermissionsTest
{
    [Fact]
    public async Task User_On_CrmCompany_Can_Access_Via_Groups()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .ToMutate()
            // Add the group `us_east_sales_399` to the team `motion_399`
            .Add<Team, Group>("motion_399", o => o.Group, "us_east_sales_399")
            // Add the user `casey_399` to the group `us_east_sales_399`
            .Add<Group, User>("us_east_sales_399", g => g.Member, "casey_399")
            // Add the team `motion_399` as an editor on the company `acme_corp_399`
            .Add<CrmCompany, Team>("acme_corp_399", c => c.Editor, "motion_399")
            // The `CrmPerson` `potential_customer` has the parent `CrmCompany` `acme_corp_399`
            .Assign<CrmPerson, CrmCompany>("potential_customer", c => c.Parent, "acme_corp_399")
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        var caseyCanAccessCrmCompany399 = await Permissions
            .WithClient(client)
            .ToValidate()
            // Now we check if `casey_399` can edit the `CrmCompany` `acme_corp_399`
            // via the group `us_east_sales_399` and the team `motion_399`
            // which has been granted editor access on the company.
            .Can<CrmCompany, User>("acme_corp_399", f => f.Perform.Edit, "casey_399")
            // Because the `CrmPerson` `potential_customer` has the parent `CrmCompany` `acme_corp_399`
            // and `casey_399` can edit the company, they should also be able to edit the person.
            .Can<CrmPerson, User>("potential_customer", f => f.Perform.Edit, "casey_399")
            .ValidateAllAsync(TestContext.Current.CancellationToken);

        Assert.True(caseyCanAccessCrmCompany399);
    }

    [Fact]
    public async Task User_That_Is_Blocked_Cannot_Access_Resource()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .ToMutate()
            // Add the group `us_east_team_400` and add `alice_400` as a member
            .Add<Group, User>("us_east_team_400", g => g.Member, "alice_400")
            .Add<Group, User>("us_east_team_400", g => g.Member, "bob_400")
            // Add the group `us_east_team_400` as an reader on the company `acme_corp_400`
            .Add<CrmCompany, Group>("acme_corp_400", c => c.Reader, "us_east_team_400")
            // Add `alice_400` directly as an owner on the company `acme_corp_400`
            .Add<CrmCompany, User>("acme_corp_400", c => c.Owner, "alice_400")
            // Now block `us_east_team_400` from accessing the company directly
            .Add<CrmCompany, Group>("acme_corp_400", c => c.Blocked, "us_east_team_400")
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        // Bob is only a member of the team and should not be able to access
        // the company because the team is blocked.
        var bobCanAccessCrmCompany = await Permissions
            .WithClient(client)
            .ToValidate()
            // Check if `bob` can access the company `acme_corp`
            .Can<CrmCompany, User>("acme_corp_400", f => f.Perform.Edit, "bob_400")
            .ValidateSingleAsync(TestContext.Current.CancellationToken);

        Assert.False(bobCanAccessCrmCompany);

        // Alice is the owner and should be able to access the company even though
        // the team is blocked.
        var aliceCanAccessCrmCompany = await Permissions
            .WithClient(client)
            .ToValidate()
            // Check if `alice` can access the company `acme_corp`
            .Has<CrmCompany, User>("acme_corp_400", f => f.Owner, "alice_400")
            .ValidateSingleAsync(TestContext.Current.CancellationToken);

        Assert.True(aliceCanAccessCrmCompany);
    }

    [Fact]
    public async Task User_That_Is_Blocked_On_Parent_Cannot_Access_Child()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .ToMutate()
            // Add the group `us_east_team_401` and add `alice_401` as a member
            .Add<Group, User>("us_east_team_401", g => g.Member, "alice_401")
            .Add<Group, User>("us_east_team_401", g => g.Member, "bob_401")
            // Add the group `us_east_team_401` as an reader on the company `acme_corp_401`
            .Add<CrmCompany, Group>("acme_corp_401", c => c.Reader, "us_east_team_401")
            // Add `alice_401` directly as an owner on the company `acme_corp_401`
            .Add<CrmCompany, User>("acme_corp_401", c => c.Owner, "alice_401")
            // Now block `us_east_team_401` from accessing the company directly
            .Add<CrmCompany, Group>("acme_corp_401", c => c.Blocked, "us_east_team_401")
            // The `CrmPerson` `potential_customer_401` has the parent `CrmCompany` `acme_corp_401`
            // So it should inherit the block from parent company.
            .Assign<CrmPerson, CrmCompany>("potential_customer_401", c => c.Parent, "acme_corp_401")
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        // Bob is only a member of the team and should not be able to access
        // the person because the team is blocked.
        var bobCanAccessCrmPerson = await Permissions
            .WithClient(client)
            .ToValidate()
            // Check if `bob` can access the person `potential_customer_401`
            .Can<CrmPerson, User>("potential_customer_401", f => f.Perform.Edit, "bob_401")
            .ValidateSingleAsync(TestContext.Current.CancellationToken);

        Assert.False(bobCanAccessCrmPerson);

        // Alice is the owner and should be able to access the company even though
        // the team is blocked.
        var aliceCanAccessCrmCompany = await Permissions
            .WithClient(client)
            .ToValidate()
            // Check if `alice` can access the company `acme_corp`
            .Has<CrmPerson, User>("potential_customer_401", f => f.Perform.Owner, "alice_401")
            .ValidateSingleAsync(TestContext.Current.CancellationToken);

        Assert.True(aliceCanAccessCrmCompany);
    }
}
