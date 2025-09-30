namespace tests;

[Collection("PermissionsCollection")]
public partial class FluentPermissionsTest
{
    private static readonly string STORE_ID = "forms-permissions-store";

    private readonly FormsPermissionsFixture _fixture;

    public FluentPermissionsTest(FormsPermissionsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Can_Write_Permission_Tuple()
    {
        var client = _fixture.GetClient(STORE_ID);

        var (added, revoked) = await Permissions
            .WithClient(client)
            .ToMutate()
            .Add<Form, User>("223", "editor", "alice")
            .Add<Form, User>("223", "editor", "bob")
            .AddMany<Form, User>("223", "reader", ["carol", "dave", "eve"])
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(5, added.Length);
        Assert.Empty(revoked);
    }

    [Fact]
    public async Task Can_Write_And_Check_Permission_Tuple()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .ToMutate()
            .Add<Form, User>("224", "editor", "alice")
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        var canAliceEdit = await Permissions
            .WithClient(client)
            .ToValidate()
            .Can<Form, User>("224", "edit", "alice")
            .ValidateSingleAsync(TestContext.Current.CancellationToken);

        Assert.True(canAliceEdit);
    }

    [Fact]
    public async Task Can_Write_And_Check_Permission_Tuple_After_Revoke()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .ToMutate()
            .Add<Form, User>("225", "editor", "alice")
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        await Permissions
            .WithClient(client)
            .ToMutate()
            .Revoke<Form, User>("225", "editor", "alice")
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        var canAliceEdit = await Permissions
            .WithClient(client)
            .ToValidate()
            .Can<Form, User>("225", "edit", "alice")
            .ValidateSingleAsync(TestContext.Current.CancellationToken);

        Assert.False(canAliceEdit);
    }

    [Fact]
    public async Task Can_Validate_Multiple_Permissions_For_User()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .ToMutate()
            .Add<Form, User>("226", "editor", "alice")
            .Add<Team, User>("motion", "member", "alice")
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        var allAllowed = await Permissions
            .WithClient(client)
            .ToValidate()
            .Can<Form, User>("226", "edit", "alice")
            .Has<Team, User>("motion", "member", "alice")
            .ValidateAllAsync(TestContext.Current.CancellationToken);

        Assert.True(allAllowed);
    }

    [Fact]
    public async Task User_Fails_Validation_If_Not_All_Grants_Match()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .ToMutate()
            .Add<Form, User>("235", "editor", "alice")
            .Add<Team, User>("acme_235", "member", "alice") // 👈 Alice is now in Acme.
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        var allAllowed = await Permissions
            .WithClient(client)
            .ToValidate()
            .Can<Form, User>("235", "edit", "alice")
            .Has<Team, User>("motion_235", "member", "alice") // 👈 Alice is NOT in Motion.
            .ValidateAllAsync(TestContext.Current.CancellationToken);

        Assert.False(allAllowed);
    }

    [Fact]
    public async Task User_Succeeds_Validation_If_At_Least_One_Grants_Match()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .ToMutate()
            .Add<Form, User>("239", "editor", "alice")
            .Add<Team, User>("acme_239", "member", "alice") // 👈 Alice is now in Acme.
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        var someAllowed = await Permissions
            .WithClient(client)
            .ToValidate()
            .Can<Form, User>("239", "edit", "alice")
            .HasAlso<Team, User>("motion_239", "member") // 👈 Alice is NOT in Motion.
            .ValidateAnyAsync(TestContext.Current.CancellationToken);

        Assert.True(someAllowed);
    }

    [Fact]
    public async Task Can_List_Objects_For_User()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .ToMutate()
            .Add<Form, User>("240", "editor", "alice")
            .Add<Team, User>("acme_240", "member", "alice") // 👈 Alice is now in Acme.
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        var objects = await Permissions
            .WithClient(client)
            .ToIntrospect()
            .ListObjectsForUserAsync<Form, User>(
                "alice",
                "editor",
                TestContext.Current.CancellationToken
            );

        // Only a single object
        Assert.Contains("form:240", objects);
    }

    [Fact]
    public async Task Can_Add_Multiple_Permissions_For_User()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .ToMutate()
            .Add<Form, User>("241", r => r.Editor, "alice")
            .AddAlso<Form, User>("242", r => r.Editor)
            .AddAlso<Team, User>("acme_241", r => r.Member)
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        var accessToAll = await Permissions
            .WithClient(client)
            .ToValidate()
            .Can<Form, User>("241", r => r.Perform.Edit, "alice")
            .CanAlso<Form, User>("242", r => r.Perform.Edit)
            .ValidateAllAsync(TestContext.Current.CancellationToken);

        // Only a single object
        Assert.True(accessToAll);
    }

    /// <summary>
    /// This test case shows transitive assignment of permissions where an org
    /// has a group, the group has a user, and an entitlement is created on the
    /// form for the org.  A user in the group should be able to access the form.
    /// </summary>
    [Fact]
    public async Task Can_Check_Transitive_Permissions_Via_Group_Fluently()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .ToMutate()
            .Add<Team, Group>("motion_299", o => o.Group, "managers_team_299")
            .Add<Group, User>("managers_team_299", g => g.Member, "casey_299")
            .Add<Form, Team>("299", f => f.Editor, "motion_299")
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        var caseyCanAccessForm299 = await Permissions
            .WithClient(client)
            .ToValidate()
            .Can<Form, User>("299", f => f.Perform.Edit, "casey_299")
            .ValidateAllAsync(TestContext.Current.CancellationToken);

        Assert.True(caseyCanAccessForm299);
    }

    [Fact]
    public async Task Can_Create_Group_And_List_Members()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Groups
            .WithClient(client)
            .AddMembersAsync(
                "engineering_team_300",
                ["alice_300", "bob_300", "carol_300"],
                TestContext.Current.CancellationToken
            );

        var members = await Groups
            .WithClient(client)
            .ListMembersAsync("engineering_team_300", TestContext.Current.CancellationToken);

        Assert.Contains("alice_300", members);
        Assert.Contains("bob_300", members);
        Assert.Contains("carol_300", members);
    }

    [Fact]
    public async Task Can_List_Objects_User_Can_Access_For_Resource_Type()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Resources
            .WithClient(client)
            .AddUsersAsync<Form, User>(
                "form_301",
                r => r.Editor,
                ["alice_301"],
                TestContext.Current.CancellationToken
            );

        await Resources
            .WithClient(client)
            .AddUsersAsync<Form, User>(
                "form_302",
                r => r.Editor,
                ["alice_301"],
                TestContext.Current.CancellationToken
            );

        await Resources
            .WithClient(client)
            .AddUsersAsync<Form, User>(
                "form_303",
                r => r.Editor,
                ["bob_301"],
                TestContext.Current.CancellationToken
            );

        var forms = await Users
            .WithClient(client)
            .ListObjectsAsync<Form>("alice_301", TestContext.Current.CancellationToken);

        Assert.Contains("form:form_301", forms);
        Assert.Contains("form:form_302", forms);
        Assert.DoesNotContain("form:form_303", forms);
    }

    [Fact]
    public async Task Can_List_All_Users_For_A_Given_Resource()
    {
        var client = _fixture.GetClient(STORE_ID);

        var resources = Resources.WithClient(client);

        await resources.AddUsersAsync<Form, User>(
            "form_303",
            r => r.Editor,
            ["alice_303"],
            TestContext.Current.CancellationToken
        );

        await resources.AddUsersAsync<Form, User>(
            "form_303",
            r => r.Editor,
            ["bob_303"],
            TestContext.Current.CancellationToken
        );

        var users = await resources.ListUsersAsync<Form>(
            "form_303",
            TestContext.Current.CancellationToken
        );

        Assert.Contains("user:alice_303", users);
        Assert.Contains("user:bob_303", users);
    }
}
