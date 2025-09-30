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
}
