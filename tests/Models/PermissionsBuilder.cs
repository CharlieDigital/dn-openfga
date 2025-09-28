using System.Linq.Expressions;
using System.Security;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Model;
using static Permissions;

/// <summary>
/// Builder class to create a set of permissions and grant them in one call.
/// </summary>
public partial class PermissionBuilder(OpenFgaClient client, bool disableTransactions = false)
{
    private readonly List<(string ObjectId, string Relation, string UserId)> _newGrants = [];
    private readonly List<(string ObjectId, string Relation, string UserId)> _removedGrants = [];
    private string? _lastUserId;

    /// <summary>
    /// Takes an expression for the resource type to get the property name.
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relationExpression">The relation that is linking the user to the object.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    /// <returns>The permission builder to continue to chain.</returns>
    public PermissionBuilder Add<TRes, TUser>(
        string objectId,
        Expression<Func<TRes, object>> relationExpression,
        string userId
    )
        where TRes : Res
        where TUser : Accessor =>
        Add<TRes, TUser>(objectId, relationExpression.ResolveName(), userId);

    /// <summary>
    /// Add a single relation.
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relation">The relation that is linking the user to the object.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    /// <returns>The permission builder to continue to chain.</returns>
    public PermissionBuilder Add<TRes, TUser>(string objectId, string relation, string userId)
        where TRes : Res
        where TUser : Accessor
    {
        var resource = MakeEntityName<TRes>(objectId);
        var user = MakeEntityName<TUser>(userId);
        _lastUserId = userId;

        _newGrants.Add(($"{resource}", relation, $"{user}"));
        return this;
    }

    /// <summary>
    /// Add a single relation for the last user added.
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relation">The relation that is linking the user to the object.</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    /// <returns>The permission builder to continue to chain.</returns>
    public PermissionBuilder AddAlso<TRes, TUser>(string objectId, string relation)
        where TRes : Res
        where TUser : Accessor
    {
        if (_lastUserId == null)
        {
            throw new InvalidOperationException("No previous user to add relation for.");
        }

        return Add<TRes, TUser>(objectId, relation, _lastUserId);
    }

    /// <summary>
    /// Add a single relation for the last user added.
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relationExpression">The relation that is linking the user to the object.</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    /// <returns>The permission builder to continue to chain.</returns>
    public PermissionBuilder AddAlso<TRes, TUser>(
        string objectId,
        Expression<Func<TRes, object>> relationExpression
    )
        where TRes : Res
        where TUser : Accessor
    {
        if (_lastUserId == null)
        {
            throw new InvalidOperationException("No previous user to add relation for.");
        }

        return Add<TRes, TUser>(objectId, relationExpression.ResolveName(), _lastUserId);
    }

    /// <summary>
    /// Convenience method to add the same relation for multiple users.
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.k</param>
    /// <param name="relation">The relation that is linking the user to the object.</param>
    /// <param name="userId">The IDs of multiple accessors (can be a group, for example).  MUST BE OF THE SAME TYPE</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    /// <returns>The permission builder to continue to chain.</returns>
    public PermissionBuilder AddMany<TRes, TUser>(
        string objectId,
        string relation,
        params string[] userIds
    )
        where TRes : Res
        where TUser : Accessor
    {
        foreach (var userId in userIds)
        {
            Add<TRes, TUser>(objectId, relation, userId);
        }

        return this;
    }

    /// <summary>
    /// Revokes a permission for a single user.  Revoke will fail if the relation
    /// does not exist.
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.k</param>
    /// <param name="relation">The relation that is linking the user to the object.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    /// <returns>The permission builder to continue to chain.</returns>
    public PermissionBuilder Revoke<TRes, TUser>(string objectId, string relation, string userId)
        where TRes : Res
        where TUser : Accessor
    {
        var resource = MakeEntityName<TRes>(objectId);
        var user = MakeEntityName<TUser>(userId);

        _removedGrants.Add(($"{resource}", relation, $"{user}"));
        return this;
    }

    /// <summary>
    /// Revokes a permission for multiple users. Revoke will fail if the relation
    /// does not exist.
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.k</param>
    /// <param name="relation">The relation that is linking the user to the object.</param>
    /// <param name="userIds">The IDs of multiple accessors (can be a group, for example).  MUST BE OF THE SAME TYPE</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    /// <returns>The permission builder to continue to chain.</returns>
    public PermissionBuilder RevokeMany<TRes, TUser>(
        string objectId,
        string relation,
        params string[] userIds
    )
        where TRes : Res
        where TUser : Accessor
    {
        foreach (var userId in userIds)
        {
            Revoke<TRes, TUser>(objectId, relation, userId);
        }

        return this;
    }

    /// <summary>
    /// Makes a bulk update to grant all the permissions added to the builder.
    /// </summary>
    /// <param name="cancellation">A cancellation token, if available.</param>
    /// <returns>An awaitable `Task`.</returns>
    public async Task<(TupleKey[] Added, TupleKey[] Revoked)> SaveChangesAsync(
        CancellationToken cancellation = default
    )
    {
        if (client == null)
        {
            throw new SecurityException("OpenFgaClient instance is required to grant permissions.");
        }

        var tuples = _newGrants
            .Select(p => new ClientTupleKey
            {
                Object = p.ObjectId,
                Relation = p.Relation,
                User = p.UserId,
            })
            .ToList();

        var removedTuples = _removedGrants
            .Select(p => new ClientTupleKeyWithoutCondition
            {
                Object = p.ObjectId,
                Relation = p.Relation,
                User = p.UserId,
            })
            .ToList();

        var request = (tuples.Count, removedTuples.Count) switch
        {
            (0, 0) => throw new InvalidOperationException("No changes to save."),
            (0, _) => new ClientWriteRequest { Deletes = removedTuples },
            (_, 0) => new ClientWriteRequest { Writes = tuples },
            _ => new ClientWriteRequest { Writes = tuples, Deletes = removedTuples },
        };

        var response = await client.Write(
            request,
            new ClientWriteOptions
            {
                // Disable transactions is false by default, but allow true to eas unit
                // testing where we need to remove in the same flow on the client.
                Transaction = new TransactionOptions { Disable = disableTransactions },
            },
            cancellationToken: cancellation
        );

        return (
            response.Writes?.Select(t => t.TupleKey).ToArray() ?? [],
            response.Deletes?.Select(t => t.TupleKey).ToArray() ?? []
        );
    }
}
