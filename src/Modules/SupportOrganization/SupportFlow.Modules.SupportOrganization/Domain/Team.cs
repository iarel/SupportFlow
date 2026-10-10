using SupportFlow.BuildingBlocks.Domain;

namespace SupportFlow.Modules.SupportOrganization.Domain;

/// <summary>
/// Team and its queue, domain-model §4. Minimal for now: only what routing of new conversations needs. Whether
/// Team is part of the MVP is open question Q2 (domain-model §13) [Assumption]; supervisors and the "at least one
/// Supervisor" invariant come with the use cases that need them.
/// </summary>
internal sealed class Team : AggregateRoot
{
    public Team(Guid id, string name, bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Team name must not be empty.");
        }

        Id = id;
        Name = name.Trim();
        IsDefault = isDefault;
    }

    public Guid Id { get; }

    public string Name { get; }

    /// <summary>
    /// New conversations go to the default team (domain-model §4, MVP routing).
    /// </summary>
    public bool IsDefault { get; }
}
