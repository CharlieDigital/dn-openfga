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
            .Add<Group, Team>("us_east_sales_399", o => o.Group, "motion_399")
            // Add the user `casey_399` to the group `us_east_sales_399`
            .Add<User, Group>("casey_399", g => g.Member, "us_east_sales_399")
            // Add the team `motion_399` as an editor on the company `acme_corp_399`
            .Add<Team, CrmCompany>("motion_399", c => c.Editor, "acme_corp_399")
            // The `CrmPerson` `potential_customer` has the parent `CrmCompany` `acme_corp_399`
            .Assign<CrmCompany, CrmPerson>("acme_corp_399", c => c.Parent, "potential_customer")
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        var caseyCanAccessCrmCompany399 = await Permissions
            .WithClient(client)
            .ToValidate()
            // Now we check if `casey_399` can edit the `CrmCompany` `acme_corp_399`
            // via the group `us_east_sales_399` and the team `motion_399`
            // which has been granted editor access on the company.
            .Can<User, CrmCompany>("casey_399", f => f.Perform.Edit, "acme_corp_399")
            // Because the `CrmPerson` `potential_customer` has the parent `CrmCompany` `acme_corp_399`
            // and `casey_399` can edit the company, they should also be able to edit the person.
            .Can<User, CrmPerson>("casey_399", f => f.Perform.Edit, "potential_customer")
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
            .Add<User, Group>("alice_400", g => g.Member, "us_east_team_400")
            .Add<User, Group>("bob_400", g => g.Member, "us_east_team_400")
            // Add the group `us_east_team_400` as an reader on the company `acme_corp_400`
            .Add<Group, CrmCompany>("us_east_team_400", c => c.Reader, "acme_corp_400")
            // Add `alice_400` directly as an owner on the company `acme_corp_400`
            .Add<User, CrmCompany>("alice_400", c => c.Owner, "acme_corp_400")
            // Now block `us_east_team_400` from accessing the company directly
            .Add<Group, CrmCompany>("us_east_team_400", c => c.Blocked, "acme_corp_400")
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        // Bob is only a member of the team and should not be able to access
        // the company because the team is blocked.
        var bobCanAccessCrmCompany = await Permissions
            .WithClient(client)
            .ToValidate()
            // Check if `bob` can access the company `acme_corp`
            .Can<User, CrmCompany>("bob_400", f => f.Editor, "acme_corp_400")
            .ValidateSingleAsync(TestContext.Current.CancellationToken);

        Assert.False(bobCanAccessCrmCompany);

        // Alice is the owner and should be able to access the company even though
        // the team is blocked.
        var aliceCanAccessCrmCompany = await Permissions
            .WithClient(client)
            .ToValidate()
            // Check if `alice` can access the company `acme_corp`
            .Has<User, CrmCompany>("alice_400", c => c.Owner, "acme_corp_400")
            .ValidateSingleAsync(TestContext.Current.CancellationToken);

        Assert.True(aliceCanAccessCrmCompany);
    }

    [Fact]
    public async Task User_That_Is_Blocked_On_Parent_Cannot_Access_Child()
    {
        var client = _fixture.GetClient(STORE_ID);

        var permissions = Permissions.WithClient(client);

        await permissions
            .ToMutate()
            // Add the group `us_east_team_401` and add `alice_401` as a member
            .Add<User, Group>("alice_401", g => g.Member, "us_east_team_401")
            .Add<User, Group>("bob_401", g => g.Member, "us_east_team_401")
            // Add the group `us_east_team_401` as an reader on the company `acme_corp_401`
            .Add<Group, CrmCompany>("us_east_team_401", c => c.Reader, "acme_corp_401")
            // Add `alice_401` directly as an owner on the company `acme_corp_401`
            .Add<User, CrmCompany>("alice_401", c => c.Owner, "acme_corp_401")
            // The `CrmPerson` `potential_customer_401` has the parent `CrmCompany` `acme_corp_401`
            // So it should inherit the block from parent company.
            .Assign<CrmCompany, CrmPerson>("acme_corp_401", c => c.Parent, "potential_customer_401")
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        // Bob is only a member of the group and should be able to access via the group
        var bobCanAccessCrmPerson = await permissions
            .ToValidate()
            // Check if `bob` can access the person `potential_customer_401`
            .Can<User, CrmPerson>("bob_401", p => p.Perform.Read, "potential_customer_401")
            .ValidateSingleAsync(TestContext.Current.CancellationToken);

        Assert.True(bobCanAccessCrmPerson);

        // Now block the group and Bob should be blocked
        await permissions
            .ToMutate()
            // Now block `us_east_team_401` from accessing the company directly
            .Add<Group, CrmCompany>("us_east_team_401", c => c.Blocked, "acme_corp_401")
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        var bobCanStillAccessCrmPerson = await permissions
            .ToValidate()
            .Can<User, CrmPerson>("bob_401", p => p.Perform.Read, "potential_customer_401")
            .ValidateSingleAsync(TestContext.Current.CancellationToken);

        Assert.False(bobCanStillAccessCrmPerson);

        // Alice is the owner and should be able to access the customer record even though
        // the team is blocked.
        var aliceCanAccessCrmCompany = await permissions
            .ToValidate()
            // Check if `alice` can access the customer record `potential_customer_401`
            .Has<User, CrmPerson>("alice_401", p => p.Perform.Owner, "potential_customer_401")
            .ValidateSingleAsync(TestContext.Current.CancellationToken);

        Assert.True(aliceCanAccessCrmCompany);
    }
}
