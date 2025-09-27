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
            .Mutate()
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
            .Mutate()
            .Add<Form, User>("224", "editor", "alice")
            .SaveChangesAsync(CancellationToken.None);

        var canAliceEdit = await Permissions
            .WithClient(client)
            .Validate()
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
            .Mutate()
            .Add<Form, User>("225", "editor", "alice")
            .SaveChangesAsync(CancellationToken.None);

        await Permissions
            .WithClient(client)
            .Mutate()
            .Revoke<Form, User>("225", "editor", "alice")
            .SaveChangesAsync(CancellationToken.None);

        var canAliceEdit = await Permissions
            .WithClient(client)
            .Validate()
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
            .Mutate()
            .Add<Form, User>("226", "editor", "alice")
            .Add<Org, User>("motion", "member", "alice")
            .SaveChangesAsync(CancellationToken.None);

        var allAllowed = await Permissions
            .WithClient(client)
            .Validate()
            .Can<Form, User>("226", "edit", "alice")
            .Can<Org, User>("motion", "member", "alice")
            .ValidateAllAsync(CancellationToken.None);

        Assert.True(allAllowed);
    }

    [Fact]
    public async Task User_Fails_Validation_If_Not_All_Grants_Match()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .Mutate()
            .Add<Form, User>("235", "editor", "alice")
            .Add<Org, User>("acme_235", "member", "alice") // 👈 Alice is now in Acme.
            .SaveChangesAsync(CancellationToken.None);

        var allAllowed = await Permissions
            .WithClient(client)
            .Validate()
            .Can<Form, User>("235", "edit", "alice")
            .Has<Org, User>("motion_235", "member", "alice") // 👈 Alice is NOT in Motion.
            .ValidateAllAsync(CancellationToken.None);

        Assert.False(allAllowed);
    }

    [Fact]
    public async Task User_Succeeds_Validation_If_At_Least_Onel_Grants_Match()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .Mutate()
            .Add<Form, User>("239", "editor", "alice")
            .Add<Org, User>("acme_239", "member", "alice") // 👈 Alice is now in Acme.
            .SaveChangesAsync(CancellationToken.None);

        var someAllowed = await Permissions
            .WithClient(client)
            .Validate()
            .Can<Form, User>("239", "edit", "alice")
            .Has<Org, User>("motion_239", "member", "alice") // 👈 Alice is NOT in Motion.
            .ValidateAnyAsync(CancellationToken.None);

        Assert.True(someAllowed);
    }

    [Fact]
    public async Task Can_List_Objects_For_User()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Permissions
            .WithClient(client)
            .Mutate()
            .Add<Form, User>("240", "editor", "alice")
            .Add<Org, User>("acme_240", "member", "alice") // 👈 Alice is now in Acme.
            .SaveChangesAsync(CancellationToken.None);

        var objects = await Permissions
            .WithClient(client)
            .Introspect()
            .ListObjectsForUserAsync<Form, User>("alice", "editor", CancellationToken.None);

        // Only a single object
        Assert.Contains("form:240", objects);
    }
}
