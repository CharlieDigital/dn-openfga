/// <summary>
/// Base type for resources
/// </summary>
public abstract record Res;

/// <summary>
/// This is a form resource type; we only use it for the name.
/// </summary>
public sealed record Form(
    string Reader,
    string Editor,
    string Approver,
    string Publisher,
    (string Edit, string Read, string Approve, string Publish) Perform
) : Res;

/// <summary>
/// This is an organization resource type; we only use it for the name.
/// </summary>
public sealed record Org(string Member, string Group) : Res;

/// <summary>
/// This is a group resource type; we only use it for the name.
/// </summary>
public sealed record Group(string Member) : Res;

/// <summary>
/// Base type for accessors (users, groups, orgs)
/// </summary>
public abstract record Accessor;

/// <summary>
/// This is a user accessor type; we only use it for the name.
/// </summary>
public sealed record User : Accessor;
