using System.Linq.Expressions;
using OpenFga.Sdk.Model;
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
    /// <param name="conditionSelector">Optional function to select the condition for the relationship.</param>
    /// <typeparam name="TGroup">The type of the accessor.</typeparam>
    /// <returns>The permission builder to continue to chain.</returns>
    public PermissionBuilder Assign<TGroup, TTarget>(
        string userId,
        string relation,
        string targetId,
        Func<Conditions, RelationshipCondition>? conditionSelector = null
    )
        where TGroup : IResource
        where TTarget : IResource
    {
        var user = MakeEntityName<TGroup>(userId);
        var target = MakeEntityName<TTarget>(targetId);
        _lastUserId = userId;

        _newGrants.Add(($"{target}", relation, $"{user}", conditionSelector));
        return this;
    }

    /// <summary>
    /// Assign a user to a higher order grouping.
    /// </summary>
    /// <param name="targetId">The ID of the higher level user grouping being assigned.</param>
    /// <param name="relationExpression">The relation expression.</param>
    /// <param name="userId">The ID of the user or accessor (can be a group, for example)</param>
    /// <param name="conditionSelector">Optional function to select the condition for the relationship.</param>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <typeparam name="TGroup">The type of the user.</typeparam>
    public PermissionBuilder Assign<TGroup, TTarget>(
        string userId,
        Expression<Func<TTarget, object>> relationExpression,
        string targetId,
        Func<Conditions, RelationshipCondition>? conditionSelector = null
    )
        where TGroup : IResource
        where TTarget : IResource =>
        Assign<TGroup, TTarget>(
            userId,
            relationExpression.ResolveName(),
            targetId,
            conditionSelector
        );

    /// <summary>
    /// Assign an additional user to a higher order grouping.
    /// </summary>
    /// <param name="targetId">The ID of the higher level user grouping being assigned.</param>
    /// <param name="relation">The relation type.</param>
    /// <param name="conditionSelector">Optional function to select the condition for the relationship.</param>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <typeparam name="TGroup">The type of the user.</typeparam>
    /// <returns></returns>
    public PermissionBuilder AssignAlso<TGroup, TTarget>(
        string userId,
        string relation,
        string targetId,
        Func<Conditions, RelationshipCondition>? conditionSelector = null
    )
        where TGroup : IResource
        where TTarget : IResource
    {
        if (_lastUserId == null)
        {
            throw new InvalidOperationException("No previous user to add relation for.");
        }

        return Assign<TGroup, TTarget>(userId, relation, targetId, conditionSelector);
    }

    /// <summary>
    /// Assign an additional user to a higher order grouping.
    /// </summary>
    /// <param name="targetId">The ID of the higher level user grouping being assigned.</param>
    /// <param name="relationExpression">The relation expression.</param>
    /// <param name="conditionSelector">Optional function to select the condition for the relationship.</param>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <typeparam name="TGroup">The type of the user.</typeparam>
    /// <returns></returns>
    public PermissionBuilder AssignAlso<TGroup, TTarget>(
        string userId,
        Expression<Func<TTarget, object>> relationExpression,
        string targetId,
        Func<Conditions, RelationshipCondition>? conditionSelector = null
    )
        where TGroup : IResource
        where TTarget : IResource
    {
        if (_lastUserId == null)
        {
            throw new InvalidOperationException("No previous user to add relation for.");
        }

        return Assign<TGroup, TTarget>(
            userId,
            relationExpression.ResolveName(),
            targetId,
            conditionSelector
        );
    }
}
