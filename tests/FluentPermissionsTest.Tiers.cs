namespace tests;

public partial class FluentPermissionsTest
{
    [Fact]
    public async Task Can_Save_Tier_Access_With_Condition()
    {
        var client = _fixture.GetClient(STORE_ID);

        var permissions = Permissions.WithClient(client);

        await permissions
            .ToMutate()
            .Add<Team, Subscription>(
                "acme_corp_2000",
                s => s.FreeTrial,
                "sub_5678",
                c => c.ForActiveTrial(TimeSpan.FromDays(10), DateTime.Parse("2026-01-01T00:00:00Z"))
            )
            .SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Can_Check_Tier_Access_With_Condition_Fluently()
    {
        var client = _fixture.GetClient(STORE_ID);

        var permissions = Permissions.WithClient(client);

        await permissions
            .ToMutate()
            .Add<Team, Subscription>(
                "acme_corp_2001",
                s => s.FreeTrial,
                "sub_5678",
                c => c.ForActiveTrial(TimeSpan.FromDays(10), DateTime.Parse("2026-01-01T00:00:00Z"))
            )
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        // Now check that we can access within the trial period
        var canAccessWithin10Days = await permissions
            .ToValidate()
            .Has<Team, Subscription>(
                "acme_corp_2001",
                s => s.FreeTrial,
                "sub_5678",
                c => c.ActiveTrialContext(DateTime.Parse("2026-01-05T00:00:00Z"))
            )
            .ValidateSingleAsync(TestContext.Current.CancellationToken);

        Assert.True(canAccessWithin10Days);
    }
}
