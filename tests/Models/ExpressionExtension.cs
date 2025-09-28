using System.Linq.Expressions;
using System.Reflection;
using Humanizer;

/// <summary>
/// Static utility to resolve the name of a property in an expression.
/// </summary>
public static class ExpressionExtension
{
    /// <summary>
    /// Resolves the name of a property in an expression.
    /// For nested properties like r => r.Perform.Read, returns only the final property name ("Read").
    /// </summary>
    /// <param name="expression">The expression to resolve.</param>
    /// <returns>The final property name if found, otherwise an empty string.</returns>
    public static string ResolveName(this Expression expression)
    {
        // Handle lambda expressions (e.g., r => r.PropertyName or r => r.Parent.Child)
        if (expression is LambdaExpression lambdaExpression)
        {
            return ResolveName(lambdaExpression.Body);
        }

        // Handle direct member access expressions (e.g., x => x.PropertyName or x => x.Parent.Child)
        if (expression is MemberExpression memberExpression)
        {
            return GetFinalMemberName(memberExpression).Underscore();
        }
        else if (
            expression is UnaryExpression unaryExpression
            && unaryExpression.Operand is MemberExpression unaryMemberExpression
        )
        {
            return GetFinalMemberName(unaryMemberExpression).Underscore();
        }

        // Return empty string if the expression doesn't match expected patterns
        return string.Empty;
    }

    /// <summary>
    /// Extracts the final member name from a member expression.
    /// For nested properties, returns only the last property in the chain.
    /// Handles tuple aliases by checking for TupleElementNames attribute.
    /// </summary>
    /// <param name="memberExpression">The member expression to extract the name from.</param>
    /// <returns>The name of the final property in the member access chain.</returns>
    private static string GetFinalMemberName(MemberExpression memberExpression)
    {
        var memberName = memberExpression.Member.Name;

        // Check if this is a tuple Item property (Item1, Item2, etc.)
        if (memberName.StartsWith("Item") && int.TryParse(memberName[4..], out int itemNumber))
        {
            // Try to get the tuple element name from the TupleElementNames attribute
            var tupleElementName = GetTupleElementName(memberExpression, itemNumber - 1);
            if (!string.IsNullOrEmpty(tupleElementName))
            {
                return tupleElementName;
            }
        }

        return memberName;
    }

    /// <summary>
    /// Gets the tuple element name from the TupleElementNames attribute if available.
    /// </summary>
    /// <param name="memberExpression">The member expression to check.</param>
    /// <param name="elementIndex">The zero-based index of the tuple element.</param>
    /// <returns>The tuple element name if found, otherwise null.</returns>
    private static string? GetTupleElementName(MemberExpression memberExpression, int elementIndex)
    {
        // Check the member itself for TupleElementNames attribute
        var tupleElementNamesAttr =
            memberExpression.Member.GetCustomAttribute<System.Runtime.CompilerServices.TupleElementNamesAttribute>();

        if (
            tupleElementNamesAttr?.TransformNames != null
            && elementIndex < tupleElementNamesAttr.TransformNames.Count
        )
        {
            return tupleElementNamesAttr.TransformNames[elementIndex];
        }

        // Check the declaring type if it's a field/property on a tuple
        if (memberExpression.Expression is MemberExpression parentMember)
        {
            var parentTupleAttr =
                parentMember.Member.GetCustomAttribute<System.Runtime.CompilerServices.TupleElementNamesAttribute>();

            if (
                parentTupleAttr?.TransformNames != null
                && elementIndex < parentTupleAttr.TransformNames.Count
            )
            {
                return parentTupleAttr.TransformNames[elementIndex];
            }
        }

        return null;
    }
}
