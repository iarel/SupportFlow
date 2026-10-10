namespace SupportFlow.Modules.SupportOrganization.Contracts;

public interface ITeamQueries
{
    /// <summary>
    /// The team that receives new conversations (<c>Team.IsDefault</c>, domain-model §4).
    /// </summary>
    Task<Guid> GetDefaultTeamIdAsync(CancellationToken cancellationToken);
}
