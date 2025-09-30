using System.Linq.Expressions;
using OpenFga.Sdk.Client;

/// <summary>
/// A wrapper around OpenFGA to make accessing core resource-scoped functionality
/// more convenient to access.
/// </summary>
public class Resources
{
    private OpenFgaClient _client;

    private Resources(OpenFgaClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Creates an instance using the provided client.
    /// </summary>
    /// <param name="client">The OpenFGA client.</param>
    public static Resources WithClient(OpenFgaClient client)
    {
        return new Resources(client);
    }

    /// <summary>
    /// Adds users to a specific resource and relation.
    /// </summary>
    /// <param name="resourceId">The ID of the resource.</param>
    /// <param name="relation">The specific relation name on that resource</param>
    /// <param name="userIds">A set of users to grant access to this resource (give the same relation)</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
    /// <typeparam name="TRes">The resource type.</typeparam>
    /// <typeparam name="TAccessor">The accessor type</typeparam>
    /// <returns>An awaitable `Task`</returns>
    public async Task AddUsersAsync<TRes, TAccessor>(
        string resourceId,
        string relation,
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default
    )
        where TRes : IResource
        where TAccessor : IAccessor
    {
        await Permissions
            .WithClient(_client)
            .ToMutate()
            .AddMany<TRes, TAccessor>(resourceId, relation, [.. userIds])
            .SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Adds users to a specific resource and relation.
    /// </summary>
    /// <param name="resourceId">The ID of the resource.</param>
    /// <param name="relationExpression">The specific relation name on that resource</param>
    /// <param name="userIds">A set of users to grant access to this resource (give the same relation)</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
    /// <typeparam name="TRes">The resource type.</typeparam>
    /// <typeparam name="TAccessor">The accessor type</typeparam>
    /// <returns>An awaitable `Task`</returns>
    public async Task AddUsersAsync<TRes, TAccessor>(
        string resourceId,
        Expression<Func<TRes, string>> relationExpression,
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default
    )
        where TRes : IResource
        where TAccessor : IAccessor =>
        await AddUsersAsync<TRes, TAccessor>(
            resourceId,
            relationExpression.ResolveName(),
            userIds,
            cancellationToken
        );
}
