using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using static Permissions;

/// <summary>
/// An instrospector for permissions; mainly to validate that the generics are working as expected.
/// </summary>
public class PermissionsIntrospector(OpenFgaClient client)
{
    /// <summary>
    /// Lists all of the objects that a user has any relation to.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="relationName">The name of the relation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <typeparam name="TUser">The type of user entity.</typeparam>
    public async Task<IEnumerable<string>> ListObjectsForUserAsync<TRes, TUser>(
        string userId,
        string relationName,
        CancellationToken cancellationToken
    )
        where TRes : Res
        where TUser : Accessor
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
}
