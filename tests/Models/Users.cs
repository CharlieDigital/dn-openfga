using System.Linq.Expressions;
using OpenFga.Sdk.Client;

/// <summary>
/// A wrapper around OpenFGA to make accessing core user-scoped functionality
/// more convenient to access.
/// </summary>
public class Users
{
    private OpenFgaClient _client;

    private Users(OpenFgaClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Users"/> class.
    /// </summary>
    /// <param name="client">The <see cref="OpenFgaClient"/> instance to use.</param>
    public static Users WithClient(OpenFgaClient client)
    {
        return new Users(client);
    }

    /// <summary>
    /// Lists all objects of a given type that the user is associated with.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="relation">The name of the relation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <typeparam name="TRes">The type of resource.</typeparam>
    /// <returns>A list of IDs which the user is associated with for the given type of resource.</returns>
    public async Task<IEnumerable<string>> ListObjectsAsync<TRes>(
        string userId,
        string relation,
        CancellationToken cancellationToken = default
    )
        where TRes : IResource
    {
        return await Permissions
            .WithClient(_client)
            .ToIntrospect()
            .ListObjectsForUserAsync<TRes, User>(userId, relation, cancellationToken);
    }

    /// <summary>
    /// Lists all objects of a given type that the user is associated with.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="relationExpression">The name of the relation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <typeparam name="TRes">The type of resource.</typeparam>
    /// <returns>A list of IDs which the user is associated with for the given type of resource.</returns>
    public async Task<IEnumerable<string>> ListObjectsAsync<TRes>(
        string userId,
        Expression<Func<TRes, string>> relationExpression,
        CancellationToken cancellationToken = default
    )
        where TRes : IResource =>
        await ListObjectsAsync<TRes>(userId, relationExpression.ResolveName(), cancellationToken);

    /// <summary>
    /// Lists all objects of a given type that the user is associated with (no
    /// restriction on the relation).
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <typeparam name="TRes">The type of resource.</typeparam>
    /// <returns>A list of IDs which the user is associated with for the given type of resource.</returns>
    public async Task<IEnumerable<string>> ListObjectsAsync<TRes>(
        string userId,
        CancellationToken cancellationToken = default
    )
        where TRes : IResource
    {
        return await Permissions
            .WithClient(_client)
            .ToIntrospect()
            .ListObjectsForUserAsync<TRes, User>(userId, cancellationToken);
    }
}
