namespace tests;

[Collection("FormsPermissionsCollection")]
public class FluentFormsPermissionsTest
{
    private static readonly string STORE_ID = "forms-permissions-store";

    private readonly FormsPermissionsFixture _fixture;

    public FluentFormsPermissionsTest(FormsPermissionsFixture fixture)
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
            .SaveChangesAsync(CancellationToken.None);

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
            .SaveChangesAsync(CancellationToken.None);

        var canAliceEdit = await Permissions
            .WithClient(client)
            .ToValidate()
            .Can<Form, User>("224", "edit", "alice")
            .ValidateSingleAsync(CancellationToken.None);

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
            .SaveChangesAsync(CancellationToken.None);

        await Permissions
            .WithClient(client)
            .ToMutate()
            .Revoke<Form, User>("225", "editor", "alice")
            .SaveChangesAsync(CancellationToken.None);

        var canAliceEdit = await Permissions
            .WithClient(client)
            .ToValidate()
            .Can<Form, User>("225", "edit", "alice")
            .ValidateSingleAsync(CancellationToken.None);

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
            .Add<Org, User>("motion", "member", "alice")
            .SaveChangesAsync(CancellationToken.None);

        var allAllowed = await Permissions
            .WithClient(client)
            .ToValidate()
            .Can<Form, User>("226", "edit", "alice")
            .Has<Org, User>("motion", "member", "alice")
            .ValidateAllAsync(CancellationToken.None);

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
            .Add<Org, User>("acme_235", "member", "alice") // 👈 Alice is now in Acme.
            .SaveChangesAsync(CancellationToken.None);

        var allAllowed = await Permissions
            .WithClient(client)
            .ToValidate()
            .Can<Form, User>("235", "edit", "alice")
            .Has<Org, User>("motion_235", "member", "alice") // 👈 Alice is NOT in Motion.
            .ValidateAllAsync(CancellationToken.None);

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
            .Add<Org, User>("acme_239", "member", "alice") // 👈 Alice is now in Acme.
            .SaveChangesAsync(CancellationToken.None);

        var someAllowed = await Permissions
            .WithClient(client)
            .ToValidate()
            .Can<Form, User>("239", "edit", "alice")
            .HasAlso<Org, User>("motion_239", "member") // 👈 Alice is NOT in Motion.
            .ValidateAnyAsync(CancellationToken.None);

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
            .Add<Org, User>("acme_240", "member", "alice") // 👈 Alice is now in Acme.
            .SaveChangesAsync(CancellationToken.None);

        var objects = await Permissions
            .WithClient(client)
            .ToIntrospect()
            .ListObjectsForUserAsync<Form, User>("alice", "editor", CancellationToken.None);

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
            .AddAlso<Org, User>("acme_241", r => r.Member)
            .SaveChangesAsync(CancellationToken.None);

        var accessToAll = await Permissions
            .WithClient(client)
            .ToValidate()
            .Can<Form, User>("241", r => r.Perform.Edit, "alice")
            .CanAlso<Form, User>("242", r => r.Perform.Edit)
            .ValidateAllAsync(CancellationToken.None);

        // Only a single object
        Assert.True(accessToAll);
    }

    [Fact]
    public async Task Can_Check_Transitive_Permissions_Via_Group_Fluenty()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .ToMutate()
            .Assign<Org, Group>("motion_299", "group", "managers_team_299")
            .Add<Group, User>("managers_team_299", "member", "casey_299")
            .Assign<Form, Org>("299", "editor", "motion_299")
            .SaveChangesAsync(CancellationToken.None);

        var caseyCanAccessForm299 = await Permissions
            .WithClient(client)
            .ToValidate()
            .Can<Form, User>("299", "edit", "casey_299")
            .ValidateSingleAsync(CancellationToken.None);

        Assert.True(caseyCanAccessForm299);
    }
}
