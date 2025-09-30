using System.Linq.Expressions;
using static Permissions;

/// <summary>
/// Partial class with the `Assign` methods which connote user-user type relations.
/// </summary>
public partial class PermissionBuilder
{
    /// <summary>
    /// Assign a user to a higher order grouping.
    /// </summary>
    /// <param name="targetId">The ID of the higher level user grouping being assigned.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <typeparam name="TGroup">The type of the accessor.</typeparam>
    /// <returns>The permission builder to continue to chain.</returns>
    public PermissionBuilder Assign<TTarget, TGroup>(
        string targetId,
        string relation,
        string userId
    )
        where TTarget : IResource
        where TGroup : IResource
    {
        var target = MakeEntityName<TTarget>(targetId);
        var user = MakeEntityName<TGroup>(userId);
        _lastUserId = userId;

        _newGrants.Add(($"{target}", relation, $"{user}"));
        return this;
    }

    /// <summary>
    /// Assign a user to a higher order grouping.
    /// </summary>
    /// <param name="targetId">The ID of the higher level user grouping being assigned.</param>
    /// <param name="relationExpression">The relation expression.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <typeparam name="TGroup">The type of the user.</typeparam>
    public PermissionBuilder Assign<TTarget, TGroup>(
        string targetId,
        Expression<Func<TTarget, object>> relationExpression,
        string userId
    )
        where TTarget : IResource
        where TGroup : IResource =>
        Assign<TTarget, TGroup>(targetId, relationExpression.ResolveName(), userId);

    /// <summary>
    /// Assign an additional user to a higher order grouping.
    /// </summary>
    /// <param name="targetId">The ID of the higher level user grouping being assigned.</param>
    /// <param name="relation">The relation type.</param>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <typeparam name="TGroup">The type of the user.</typeparam>
    /// <returns></returns>
    public PermissionBuilder AssignAlso<TTarget, TGroup>(string targetId, string relation)
        where TTarget : IResource
        where TGroup : IResource
    {
        if (_lastUserId == null)
        {
            throw new InvalidOperationException("No previous user to add relation for.");
        }

        return Assign<TTarget, TGroup>(targetId, relation, _lastUserId);
    }

    /// <summary>
    /// Assign an additional user to a higher order grouping.
    /// </summary>
    /// <param name="targetId">The ID of the higher level user grouping being assigned.</param>
    /// <param name="relationExpression">The relation expression.</param>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <typeparam name="TGroup">The type of the user.</typeparam>
    /// <returns></returns>
    public PermissionBuilder AssignAlso<TTarget, TGroup>(
        string targetId,
        Expression<Func<TTarget, object>> relationExpression
    )
        where TTarget : IResource
        where TGroup : IResource
    {
        if (_lastUserId == null)
        {
            throw new InvalidOperationException("No previous user to add relation for.");
        }

        return Assign<TTarget, TGroup>(targetId, relationExpression.ResolveName(), _lastUserId);
    }
}
