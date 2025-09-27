using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Model;

/// <summary>
/// Builder class to create a set of permissions and grant them in one call.
/// </summary>
public class PermissionChecker(OpenFgaClient client)
{
    private readonly List<(string ObjectId, string Relation, string UserId)> _checks = [];

    /// <summary>
    /// Performs a single permission check
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relation">The relation that is linking the user to the object.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    public PermissionChecker Can<TRes, TUser>(string objectId, string relation, string userId)
        where TRes : Res
        where TUser : Accessor
    {
        var resourceType = typeof(TRes).Name.ToLower();
        var userType = typeof(TUser).Name.ToLower();

        _checks.Add(($"{resourceType}:{objectId}", relation, $"{userType}:{userId}"));

        return this;
    }

    /// <summary>
    /// Performs a single permission check (alias for Can)
    /// </summary>
    /// <param name="objectId">The ID of the object that the permissions are being granted to.</param>
    /// <param name="relation">The relation that is linking the user to the object.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <typeparam name="TRes">The type of the resource.</typeparam>
    /// <typeparam name="TUser">The type of the accessor.</typeparam>
    public PermissionChecker Has<TRes, TUser>(string objectId, string relation, string userId)
        where TRes : Res
        where TUser : Accessor => Can<TRes, TUser>(objectId, relation, userId);

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
