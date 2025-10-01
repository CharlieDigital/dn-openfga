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
    private readonly List<(
        string ObjectId,
        string Relation,
        string UserId,
        Func<Conditions, RelationshipCondition>? conditionSelector
    )> _newGrants = [];
    private readonly List<(string ObjectId, string Relation, string UserId)> _removedGrants = [];
    private string? _lastUserId;

    /// <summary>
    /// Add a single relation.
    /// </summary>
    /// <remarks>
    /// This is the main underlying method.
    /// </remarks>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relation">The relation that is linking the user to the object.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <param name="conditionSelector">Optional function to select the condition for the relationship.</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    /// <returns>The permission builder to continue to chain.</returns>
    public PermissionBuilder Add<TUser, TRes>(
        string userId,
        string relation,
        string objectId,
        Func<Conditions, RelationshipCondition>? conditionSelector = null
    )
        where TUser : IAccessor
        where TRes : IResource
    {
        var user = MakeEntityName<TUser>(userId);
        var resource = MakeEntityName<TRes>(objectId);

        _lastUserId = userId;

        _newGrants.Add(($"{resource}", relation, $"{user}", conditionSelector));

        return this;
    }

    /// <summary>
    /// Takes an expression for the resource type to get the property name.
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relationExpression">The relation that is linking the user to the object.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <param name="conditionSelector">Optional function to select the condition for the relationship.</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    /// <returns>The permission builder to continue to chain.</returns>
    public PermissionBuilder Add<TUser, TRes>(
        string userId,
        Expression<Func<TRes, object>> relationExpression,
        string objectId,
        Func<Conditions, RelationshipCondition>? conditionSelector = null
    )
        where TUser : IAccessor
        where TRes : IResource =>
        Add<TUser, TRes>(userId, relationExpression.ResolveName(), objectId, conditionSelector);

    /// <summary>
    /// Add a single relation for the last user added.
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relation">The relation that is linking the user to the object.</param>
    /// <param name="conditionSelector">Optional function to select the condition for the relationship.</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    /// <returns>The permission builder to continue to chain.</returns>
    public PermissionBuilder AddAlso<TUser, TRes>(
        string relation,
        string objectId,
        Func<Conditions, RelationshipCondition>? conditionSelector = null
    )
        where TUser : IAccessor
        where TRes : IResource
    {
        if (_lastUserId == null)
        {
            throw new InvalidOperationException("No previous user to add relation for.");
        }

        return Add<TUser, TRes>(_lastUserId, relation, objectId, conditionSelector);
    }

    /// <summary>
    /// Add a single relation for the last user added.
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relationExpression">The relation that is linking the user to the object.</param>
    /// <param name="conditionSelector">Optional function to select the condition for the relationship.</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    /// <returns>The permission builder to continue to chain.</returns>
    public PermissionBuilder AddAlso<TUser, TRes>(
        Expression<Func<TRes, object>> relationExpression,
        string objectId,
        Func<Conditions, RelationshipCondition>? conditionSelector = null
    )
        where TUser : IAccessor
        where TRes : IResource
    {
        if (_lastUserId == null)
        {
            throw new InvalidOperationException("No previous user to add relation for.");
        }

        return Add<TUser, TRes>(
            _lastUserId,
            relationExpression.ResolveName(),
            objectId,
            conditionSelector
        );
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
    public PermissionBuilder AddMany<TUser, TRes>(
        string relation,
        string objectId,
        params string[] userIds
    )
        where TUser : IAccessor
        where TRes : IResource
    {
        foreach (var userId in userIds)
        {
            Add<TUser, TRes>(userId, relation, objectId);
        }

        return this;
    }

    /// <summary>
    /// Convenience method to add the same relation for multiple users.
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.k</param>
    /// <param name="relationExpression">The relation that is linking the user to the object.</param>
    /// <param name="userId">The IDs of multiple accessors (can be a group, for example).  MUST BE OF THE SAME TYPE</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    /// <returns>The permission builder to continue to chain.</returns>
    public PermissionBuilder AddMany<TUser, TRes>(
        string relation,
        Expression<Func<TRes, object>> relationExpression,
        string objectId,
        params string[] userIds
    )
        where TUser : IAccessor
        where TRes : IResource => AddMany<TUser, TRes>(relation, objectId, userIds);

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
    public PermissionBuilder Revoke<TUser, TRes>(string userId, string relation, string objectId)
        where TUser : IAccessor
        where TRes : IResource
    {
        var user = MakeEntityName<TUser>(userId);
        var resource = MakeEntityName<TRes>(objectId);

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
    public PermissionBuilder RevokeMany<TUser, TRes>(
        string relation,
        string objectId,
        params string[] userIds
    )
        where TUser : IAccessor
        where TRes : IResource
    {
        foreach (var userId in userIds)
        {
            Revoke<TUser, TRes>(userId, relation, objectId);
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

        var conditions = new Conditions();

        var tuples = _newGrants
            .Select(p => new ClientTupleKey
            {
                Object = p.ObjectId,
                Relation = p.Relation,
                User = p.UserId,
                Condition = p.conditionSelector?.Invoke(conditions),
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

        var json = request.ToJson();

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

        _newGrants.Clear();
        _removedGrants.Clear();
        _lastUserId = null;

        return (
            response.Writes?.Select(t => t.TupleKey).ToArray() ?? [],
            response.Deletes?.Select(t => t.TupleKey).ToArray() ?? []
        );
    }
}
