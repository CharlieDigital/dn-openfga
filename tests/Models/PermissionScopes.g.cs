// This file is auto-generated from fga-model.fga
#pragma warning disable SA1600 // Auto-generated code
public sealed record User : IAccessor;

public sealed record Group(
    string Member,
    string Team
) : IResource, IAccessor;

public sealed record Team(
    string Group,
    string Member
) : IResource, IAccessor;

public sealed record Form(
    string Approver,
    string Editor,
    string Publisher,
    string Reader,
    (string Approve, string Edit, string Publish, string Read) Perform
) : IResource;

public sealed record Document(
    string Approver,
    string Editor,
    string Publisher,
    string Reader,
    (string Approve, string Edit, string Publish, string Read) Perform
) : IResource;

public sealed record CrmCompany(
    string Blocked,
    string Editor,
    string Owner,
    string Reader,
    (string Edit, string Read) Perform
) : IResource;

public sealed record CrmPerson(
    string Blocked,
    string Parent,
    (string Edit, string Editor, string Owner, string Read, string Reader) Perform
) : IResource;

public sealed record CrmEmailAccount : IAccessor;

#pragma warning restore SA1600 // Auto-generated code
