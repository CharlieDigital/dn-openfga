using OpenFga.Sdk.Client.Model;

namespace tests;

public partial class PermissionsTest
{
    [Fact]
    public async Task Can_Check_Tier_Access_With_Attribute()
    {
        var client = _fixture.GetClient(STORE_ID);

        var request = new ClientWriteRequest()
        {
            Writes =
            [
                // We connect `acme_corp_1000` team to the `free_trial` tier
                new()
                {
                    Object = "subscription:sub_1234",
                    Relation = "free_trial",
                    User = "team:acme_corp_1000",
                    Condition = new()
                    {
                        Name = "active_trial",
                        Context = new
                        {
                            trial_start = "2026-01-01T00:00:00Z",
                            trial_duration = "240h",
                        },
                    },
                },
            ],
        };

        await client.Write(request, cancellationToken: CancellationToken.None);

        // Now we check using a condition
        var checkResponseWithin10Days = await client.Check(
            new ClientCheckRequest
            {
                Object = "subscription:sub_1234",
                Relation = "free_trial",
                User = "team:acme_corp_1000",
                Context = new { current_time = "2026-01-02T00:00:00Z" },
            },
            cancellationToken: CancellationToken.None
        );

        Assert.True(checkResponseWithin10Days.Allowed);

        // Now we check using a condition
        var checkResponseBeyond10Days = await client.Check(
            new ClientCheckRequest
            {
                Object = "subscription:sub_1234",
                Relation = "free_trial",
                User = "team:acme_corp_1000",
                Context = new { current_time = "2026-01-20T00:00:00Z" },
            },
            cancellationToken: CancellationToken.None
        );

        Assert.False(checkResponseBeyond10Days.Allowed);
    }
}
