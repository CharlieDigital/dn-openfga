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
    string Editor,
    string External,
    string Parent,
    string Reader,
    (string Edit, string Owner, string Read) Perform
) : IResource;

public sealed record Subscription(
    string Enterprise,
    string FreeTrial,
    string Paid,
    (string UseAgentWorkflows, string UseAiChat) Perform
) : IResource;


// Conditions which can be used in the model.
public sealed record Conditions
{
    public OpenFga.Sdk.Model.RelationshipCondition ForActiveTrial(
        TimeSpan trialDuration,
        DateTime trialStart
    )
        => new() {
            Name = "active_trial",
            Context = new
            {
                trial_duration_init = $"{trialDuration.TotalSeconds}s",
                trial_start_init = trialStart.ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ")
            }
        };
    public sealed record ReadContext
    {
        public object ActiveTrialContext(
            DateTime currentTime
        )
            => new
            {
                current_time_provided = currentTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ")
            };
    }
}
#pragma warning restore SA1600 // Auto-generated code
