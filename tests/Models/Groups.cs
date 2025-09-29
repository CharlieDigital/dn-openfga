using OpenFga.Sdk.Client;

/// <summary>
/// A static wrapper around the concept of groups which simply provides mechanism
/// that re-use the underlying <see cref="Permissions"/> layer to make it more
/// convenient to work with groups.
/// </summary>
public class Groups
{
    private OpenFgaClient _client;

    private Groups(OpenFgaClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Provides the client instance to use.
    /// </summary>
    /// <param name="client">The client instance to the OpenFGA API.</param>
    public static Groups WithClient(OpenFgaClient client)
    {
        return new Groups(client);
    }

    /// <summary>
    /// Adds a set of users to the group.
    /// </summary>
    /// <param name="groupId">The ID of the group to add members to.</param>
    /// <param name="userIds">The ID of the users to be added to the group.</param>
    /// <param name="cancellationToken">A cancellation token if available.</param>
    /// <returns>An awaitable `Task`.</returns>
    public async Task AddMembersAsync(
        string groupId,
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default
    )
    {
        await Permissions
            .WithClient(_client)
            .ToMutate()
            .AddMany<Group, User>(groupId, g => g.Member, [.. userIds])
            .SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Lists all members of the group.
    /// </summary>
    /// <param name="groupId">The ID of the group to list member IDs for.</param>
    /// <param name="cancellationToken">A cancellation token if available.</param>
    /// <returns>A list of member IDs.</returns>
    public async Task<IEnumerable<string>> ListMembersAsync(
        string groupId,
        CancellationToken cancellationToken = default
    )
    {
        return await Permissions
            .WithClient(_client)
            .ToIntrospect()
            .ListUsersForObjectAsync<Group, User>(groupId, g => g.Member, cancellationToken);
    }
}
