using System.Linq.Expressions;

/// <summary>
/// Static utility to resolve the name of a property in an expression.
/// </summary>
public static class ExpressionExtension
{
    /// <summary>
    /// Resolves the name of a property in an expression.
    /// </summary>
    /// <param name="expression">The expression to resolve.</param>
    /// <returns></returns>
    public static string ResolveName(this Expression expression)
    {
        if (expression is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }
        else if (
            expression is UnaryExpression unaryExpression
            && unaryExpression.Operand is MemberExpression unaryMemberExpression
        )
        {
            return unaryMemberExpression.Member.Name;
        }

        return string.Empty;
    }
}
