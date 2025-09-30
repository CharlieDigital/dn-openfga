using System.Linq.Expressions;
using Humanizer;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using static Permissions;

/// <summary>
/// An instrospector for permissions; mainly to validate that the generics are working as expected.
/// </summary>
public class PermissionsIntrospector(OpenFgaClient client)
{
    /// <summary>
    /// Lists all of the objects that a user has the specified relation to.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="relationName">The name of the relation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <typeparam name="TUser">The type of user entity.</typeparam>
    public async Task<IEnumerable<string>> ListObjectsForUserAsync<TRes, TUser>(
        string userId,
        string relationName,
        CancellationToken cancellationToken = default
    )
        where TRes : IResource
        where TUser : IAccessor
    {
        var user = MakeEntityName<TUser>(userId);
        var resource = MakeEntityName<TRes>();

        var request = new ClientListObjectsRequest
        {
            User = user,
            Relation = relationName,
            Type = resource,
        };

        var response = await client.ListObjects(request, cancellationToken: cancellationToken);

        return response.Objects;
    }

    /// <summary>
    /// Lists all of the objects that a user has any relation to.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <typeparam name="TUser">The type of user entity.</typeparam>
    public async Task<IEnumerable<string>> ListObjectsForUserAsync<TRes, TUser>(
        string userId,
        CancellationToken cancellationToken = default
    )
        where TRes : IResource
        where TUser : IAccessor
    {
        var user = MakeEntityName<TUser>(userId);
        var resource = MakeEntityName<TRes>();

        var request = new ClientReadRequest { User = user, Object = $"{resource}:" };

        var response = await client.Read(request, cancellationToken: cancellationToken);

        return response.Tuples.Select(t => t.Key.Object);
    }

    /// <summary>
    /// Lists all of the objects that a user has any relation to.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="relationExpression">The name of the relation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <typeparam name="TUser">The type of user entity.</typeparam>
    public async Task<IEnumerable<string>> ListObjectsForUserAsync<TRes, TUser>(
        string userId,
        Expression<Func<TRes, object>> relationExpression,
        CancellationToken cancellationToken = default
    )
        where TRes : IResource
        where TUser : IAccessor =>
        await ListObjectsForUserAsync<TRes, TUser>(
            userId,
            relationExpression.ResolveName(),
            cancellationToken
        );

    /// <summary>
    /// Gets a list of user IDs for the given object and relation.
    /// </summary>
    /// <param name="objectId">The ID of the object.</param>
    /// <param name="relationName">The relation name to list users for.</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <returns>The list of user IDs for the given object and relation.</returns>
    public async Task<IEnumerable<string>> ListUsersForObjectAsync<TRes, TUser>(
        string objectId,
        string relationName,
        CancellationToken cancellationToken = default
    )
        where TRes : IResource
        where TUser : IAccessor
    {
        var objectType = typeof(TRes).Name.Underscore();
        var userType = typeof(TUser).Name.Underscore();

        var request = new ClientListUsersRequest
        {
            Object = new() { Type = objectType, Id = objectId },
            Relation = relationName,
            UserFilters = [new() { Type = userType }],
        };

        var response = await client.ListUsers(request, cancellationToken: cancellationToken);

        return response.Users.Select(u => u.Object?.Id).Where(id => id is not null)!;
    }

    /// <summary>
    /// Gets a list of user IDs for the given object and relation.
    /// </summary>
    /// <param name="objectId">The ID of the object.</param>
    /// <param name="relationName">The relation name to list users for.</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <returns>The list of user IDs for the given object and relation.</returns>
    public async Task<IEnumerable<string>> ListUsersForObjectAsync<TRes, TUser>(
        string objectId,
        Expression<Func<TRes, object>> relationExpression,
        CancellationToken cancellationToken = default
    )
        where TRes : IResource
        where TUser : IAccessor =>
        await ListUsersForObjectAsync<TRes, TUser>(
            objectId,
            relationExpression.ResolveName(),
            cancellationToken
        );

    /// <summary>
    /// For a given object ID, get all of the users that have any relation to it.
    /// </summary>
    /// <param name="objectId">The ID of the object.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <returns>A list of all user entities with a relation to the object.</returns>
    public async Task<IEnumerable<string>> ListUsersForObjectAsync<TRes>(
        string objectId,
        CancellationToken cancellationToken = default
    )
        where TRes : IResource
    {
        var objectSpecifier = MakeEntityName<TRes>(objectId);

        var request = new ClientReadRequest { Object = objectSpecifier };

        var response = await client.Read(request, cancellationToken: cancellationToken);

        return response.Tuples.Select(u => u.Key?.User).Where(id => id is not null)!;
    }
}
