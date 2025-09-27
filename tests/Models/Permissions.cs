using Humanizer;
using OpenFga.Sdk.Client;

/// <summary>
/// Static entry point to make it easier to start building permissions.
/// </summary>
public class Permissions
{
    // <summary>
    /// Provides the client that will be used.  Returns a tuple of functions to create
    /// a permission builder and a permission checker.
    /// </summary>
    public static (
        Func<PermissionBuilder> Mutate,
        Func<PermissionChecker> Validate,
        Func<PermissionsIntrospector> Introspect
    ) WithClient(OpenFgaClient client, bool disableTransactions = false)
    {
        return (
            () => new PermissionBuilder(client, disableTransactions),
            () => new PermissionChecker(client),
            () => new PermissionsIntrospector(client)
        );
    }

    // <summary>
    /// Provides the client that will be used.  Returns a tuple of functions to create
    /// a permission builder and a permission checker.
    /// </summary>
    public static (
        Func<PermissionBuilder> Mutate,
        Func<PermissionChecker> Validate,
        Func<PermissionsIntrospector> Introspect
    ) WithClientNoTx(OpenFgaClient client)
    {
        return (
            () => new PermissionBuilder(client, true),
            () => new PermissionChecker(client),
            () => new PermissionsIntrospector(client)
        );
    }

    /// <summary>
    /// Convenience method to create an entity name from a type and ID.
    /// </summary>
    /// <param name="id">The ID for the entity.</param>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <returns>A string in lower case with underscore (snake_case).</returns>
    public static string MakeEntityName<T>(string? id = null)
    {
        var typeName = typeof(T).Name.Underscore();
        return id is not null ? $"{typeName}:{id}" : $"{typeName}";
    }
}
