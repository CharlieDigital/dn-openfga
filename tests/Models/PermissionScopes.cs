/// <summary>
/// Base type for resources
/// </summary>
public interface IResource;

/// <summary>
/// This is a form resource type; we only use it for the name.
/// </summary>
public sealed record Form(
    string Reader,
    string Editor,
    string Approver,
    string Publisher,
    (string Edit, string Read, string Approve, string Publish) Perform
) : IResource;

/// <summary>
/// This is an organization resource type; we only use it for the name.
/// </summary>
public sealed record Org(string Member, string Group) : IResource, IAccessor;

/// <summary>
/// This is a group resource type; we only use it for the name.
/// </summary>
public sealed record Group(string Member) : IResource, IAccessor;

/// <summary>
/// Base type for accessors (users, groups, orgs)
/// </summary>
public interface IAccessor;

/// <summary>
/// This is a user accessor type; we only use it for the name.
/// </summary>
public sealed record User : IAccessor;
