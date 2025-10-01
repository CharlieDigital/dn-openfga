using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Exceptions;

namespace tests;

[Collection("PermissionsCollection")]
public partial class PermissionsTest
{
    private static readonly string STORE_ID = "forms-permissions-store";

    private readonly PermissionsFixture _fixture;

    public PermissionsTest(PermissionsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Can_Write_Permission_Tuple()
    {
        var client = _fixture.GetClient(STORE_ID);

        await client.Write(
            new ClientWriteRequest(
                [
                    // Alice is an admin of form 123
                    new()
                    {
                        Object = "form:123",
                        Relation = "editor",
                        User = "user:alice",
                    },
                ]
            ),
            cancellationToken: CancellationToken.None
        );
    }

    [Fact]
    public async Task Invalid_Tuple_Throws_Exception_On_Write()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Assert.ThrowsAsync<FgaApiValidationError>(async () =>
        {
            await client.Write(
                new ClientWriteRequest(
                    [
                        // record is not part of the model.
                        new()
                        {
                            Object = "record:123",
                            Relation = "editor",
                            User = "user:alice",
                        },
                    ]
                ),
                cancellationToken: CancellationToken.None
            );
        });
    }

    [Fact]
    public async Task Invalid_Tuple_Throws_Exception_On_Check()
    {
        var client = _fixture.GetClient(STORE_ID);

        await Assert.ThrowsAsync<FgaApiValidationError>(async () =>
        {
            var checkResponse = await client.Check(
                new ClientCheckRequest
                {
                    Object = "record:124",
                    Relation = "editor",
                    User = "user:avery",
                },
                cancellationToken: CancellationToken.None
            );
        });
    }

    [Fact]
    public async Task Can_Check_Permission_Tuples()
    {
        var client = _fixture.GetClient(STORE_ID);

        await client.Write(
            new ClientWriteRequest(
                [
                    // Alice is an admin of form 123
                    new()
                    {
                        Object = "form:124",
                        Relation = "editor",
                        User = "user:avery",
                    },
                ]
            ),
            cancellationToken: CancellationToken.None
        );

        var checkResponse = await client.Check(
            new ClientCheckRequest
            {
                Object = "form:124",
                Relation = "editor",
                User = "user:avery",
            },
            cancellationToken: CancellationToken.None
        );

        Assert.True(checkResponse.Allowed);

        var checkResponse2 = await client.Check(
            new ClientCheckRequest
            {
                Object = "form:125",
                Relation = "editor",
                User = "user:avery",
            },
            cancellationToken: CancellationToken.None
        );

        Assert.False(checkResponse2.Allowed);
    }

    [Fact]
    public async Task Can_Check_Member_In_Group()
    {
        var client = _fixture.GetClient(STORE_ID);

        // Bob is a member of group:managers
        await client.Write(
            new ClientWriteRequest(
                [
                    new()
                    {
                        Object = "group:managers",
                        Relation = "member",
                        User = "user:bob",
                    },
                ]
            ),
            cancellationToken: CancellationToken.None
        );

        // Check if Bob has editor access to form:126
        var checkResponse = await client.Check(
            new ClientCheckRequest
            {
                Object = "group:managers",
                Relation = "member",
                User = "user:bob",
            },
            cancellationToken: CancellationToken.None
        );

        Assert.True(checkResponse.Allowed);
    }

    [Fact]
    public async Task Can_Check_Transitive_Permissions_Via_Group()
    {
        var client = _fixture.GetClient(STORE_ID);

        // Casey is a member of group:managers
        await client.Write(
            new ClientWriteRequest(
                [
                    new()
                    {
                        Object = "group:managers",
                        Relation = "member",
                        User = "user:casey",
                    },
                ]
            ),
            cancellationToken: CancellationToken.None
        );

        // Check if Casey is a member of group:managers
        var isMemberResponse = await client.Check(
            new ClientCheckRequest
            {
                Object = "group:managers",
                Relation = "member",
                User = "user:casey",
            },
            cancellationToken: CancellationToken.None
        );

        Assert.True(isMemberResponse.Allowed);

        // Managers have edit permissions on form:127
        await client.Write(
            new ClientWriteRequest(
                [
                    new()
                    {
                        Object = "form:127",
                        Relation = "editor",
                        User = "group:managers",
                    },
                ]
            ),
            cancellationToken: CancellationToken.None
        );

        // Check if group:managers has edit access to form:127
        var hasEditorPermissionResponse = await client.Check(
            new ClientCheckRequest
            {
                Object = "form:127",
                Relation = "editor",
                User = "group:managers",
            },
            cancellationToken: CancellationToken.None
        );

        Assert.True(hasEditorPermissionResponse.Allowed);

        // Check if Casey has edit access to form:127
        var checkResponse = await client.Check(
            new ClientCheckRequest
            {
                Object = "form:127",
                Relation = "edit",
                User = "user:casey",
            },
            cancellationToken: CancellationToken.None
        );

        Assert.True(checkResponse.Allowed);
    }

    [Fact]
    public async Task Can_Check_Transitive_Permissions_Via_Group_And_Team()
    {
        var client = _fixture.GetClient(STORE_ID);

        // Managers are part of team:motion
        await client.Write(
            new ClientWriteRequest(
                [
                    new()
                    {
                        Object = "team:motion",
                        Relation = "group",
                        User = "group:managers_team_123",
                    },
                ]
            ),
            cancellationToken: CancellationToken.None
        );

        // org:motion has edit permissions on form:127
        await client.Write(
            new ClientWriteRequest(
                [
                    new()
                    {
                        Object = "form:127",
                        Relation = "editor",
                        User = "team:motion",
                    },
                ]
            ),
            cancellationToken: CancellationToken.None
        );

        // Casey is a member of group:managers
        await client.Write(
            new ClientWriteRequest(
                [
                    new()
                    {
                        Object = "group:managers_team_123",
                        Relation = "member",
                        User = "user:casey",
                    },
                ]
            ),
            cancellationToken: CancellationToken.None
        );

        // Check if Casey has edit access to form:127
        var checkResponse = await client.Check(
            new ClientCheckRequest
            {
                Object = "form:127",
                Relation = "edit",
                User = "user:casey",
            },
            cancellationToken: CancellationToken.None
        );

        Assert.True(checkResponse.Allowed);
    }
}
