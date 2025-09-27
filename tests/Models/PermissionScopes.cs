/// <summary>
/// Base type for resources
/// </summary>
public abstract record Res;

/// <summary>
/// This is a form resource type; we only use it for the name.
/// </summary>
public sealed record Form : Res;

/// <summary>
/// This is an organization resource type; we only use it for the name.
/// </summary>
public sealed record Org : Res;

/// <summary>
/// Base type for accessors (users, groups, orgs)
/// </summary>
public abstract record Accessor;

/// <summary>
/// This is a user accessor type; we only use it for the name.
/// </summary>
public sealed record User : Accessor;
