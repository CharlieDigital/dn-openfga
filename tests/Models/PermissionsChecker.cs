using System.Linq.Expressions;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Model;
using static Permissions;

/// <summary>
/// Builder class to create a set of permissions and grant them in one call.
/// </summary>
public class PermissionChecker(OpenFgaClient client)
{
    private readonly List<(string ObjectId, string Relation, string UserId)> _checks = [];
    private string? _lastUserId;

    /// <summary>
    /// Performs a single permission check using a typed relation expression.
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relationExpression">The relation expression that is linking the user to the object.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    public PermissionChecker Can<TUser, TRes>(
        string userId,
        Expression<Func<TRes, object>> relationExpression,
        string objectId
    )
        where TUser : IAccessor
        where TRes : IResource =>
        Can<TUser, TRes>(userId, relationExpression.ResolveName(), objectId);

    /// <summary>
    /// Performs a single permission check
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relation">The relation that is linking the user to the object.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    public PermissionChecker Can<TUser, TRes>(string userId, string relation, string objectId)
        where TUser : IAccessor
        where TRes : IResource
    {
        var userIdentifier = MakeEntityName<TUser>(userId);
        var resourceIdentifier = MakeEntityName<TRes>(objectId);
        _lastUserId = userId;

        _checks.Add((resourceIdentifier, relation, userIdentifier));

        return this;
    }

    /// <summary>
    /// Performs a single permission check for the last user checked.
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relation">The relation that is linking the user to the object.</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    public PermissionChecker CanAlso<TUser, TRes>(string userId, string relation)
        where TUser : IAccessor
        where TRes : IResource
    {
        if (_lastUserId == null)
        {
            throw new InvalidOperationException("No previous user to add relation for.");
        }

        return Can<TUser, TRes>(userId, relation, _lastUserId);
    }

    /// <summary>
    /// Performs a single permission check for the last user checked.
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relationExpression">The relation expression that is linking the user to the object.</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    public PermissionChecker CanAlso<TUser, TRes>(
        string userId,
        Expression<Func<TRes, object>> relationExpression,
        string objectId
    )
        where TUser : IAccessor
        where TRes : IResource
    {
        if (_lastUserId == null)
        {
            throw new InvalidOperationException("No previous user to add relation for.");
        }

        return Can<TUser, TRes>(userId, relationExpression.ResolveName(), _lastUserId);
    }

    /// <summary>
    /// Performs a single permission check (alias for Can)
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relation">The relation that is linking the user to the object.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    public PermissionChecker Has<TUser, TRes>(string userId, string relation, string objectId)
        where TUser : IAccessor
        where TRes : IResource => Can<TUser, TRes>(userId, relation, objectId);

    /// <summary>
    /// Performs a single permission check (alias for Can)
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relationExpression">The relation that is linking the user to the object.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    public PermissionChecker Has<TUser, TRes>(
        string userId,
        Expression<Func<TRes, object>> relationExpression,
        string objectId
    )
        where TUser : IAccessor
        where TRes : IResource => Can<TUser, TRes>(userId, relationExpression, objectId);

    /// <summary>
    /// Performs a single permission check (alias for Can) using the last user checked.
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relation">The relation that is linking the user to the object.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    public PermissionChecker HasAlso<TUser, TRes>(string userId, string relation)
        where TUser : IAccessor
        where TRes : IResource => CanAlso<TUser, TRes>(userId, relation);

    /// <summary>
    /// Performs a single permission check (alias for Can)
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relationExpression">The relation expression that is linking the user to the object.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    public PermissionChecker HasAlso<TUser, TRes>(
        string userId,
        Expression<Func<TRes, object>> relationExpression,
        string objectId
    )
        where TUser : IAccessor
        where TRes : IResource => CanAlso<TUser, TRes>(userId, relationExpression, objectId);

    /// <summary>
    /// Validates the first permission check that was added.  Ignores any others.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The validation result; false if there were no checks added.</returns>
    public async Task<bool> ValidateSingleAsync(CancellationToken cancellationToken)
    {
        if (_checks.Count == 0)
        {
            return false;
        }

        var (objectId, relation, userId) = _checks[0];

        var response = await client.Check(
            new ClientCheckRequest
            {
                Object = objectId,
                Relation = relation,
                User = userId,
            },
            cancellationToken: cancellationToken
        );

        _checks.Clear();
        _lastUserId = null;

        return response.Allowed ?? false;
    }

    /// <summary>
    /// Validates all of the permission checks that were added.  Uses a batch check
    /// mechanism.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An array of validation results which includes a listing of allowed + the resource information.</returns>
    public async Task<(bool Allowed, TupleKey Details)[]> ValidateAsync(
        CancellationToken cancellationToken
    )
    {
        var request = _checks
            .Select(
                (t) =>
                    new ClientCheckRequest
                    {
                        Object = t.ObjectId,
                        Relation = t.Relation,
                        User = t.UserId,
                    }
            )
            .ToList();

        var response = await client.BatchCheck(request, cancellationToken: cancellationToken);

        _checks.Clear();
        _lastUserId = null;

        return
        [
            .. response.Responses.Select(r =>
                (r.Allowed, new TupleKey(r.Request.Relation, r.Request.User, r.Request.Object))
            ),
        ];
    }

    /// <summary>
    /// Validates all of the permission checks that were added.  Uses a batch check
    /// mechanism and returns a true value if all checks pass.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An boolean which indicates if all of the entitlement checks resolved to true.</returns>
    public async Task<bool> ValidateAllAsync(CancellationToken cancellationToken = default)
    {
        var results = await ValidateAsync(cancellationToken);
        var allAllowed = results.All(r => r.Allowed);
        return allAllowed;
    }

    /// <summary>
    /// Validates any of the permission checks that were added.  Uses a batch check
    /// mechanism and returns a true value if any checks pass.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An boolean which indicates if any of the entitlement checks resolved to true.</returns>
    public async Task<bool> ValidateAnyAsync(CancellationToken cancellationToken = default)
    {
        var results = await ValidateAsync(cancellationToken);
        var anyAllowed = results.Any(r => r.Allowed);
        return anyAllowed;
    }
}
