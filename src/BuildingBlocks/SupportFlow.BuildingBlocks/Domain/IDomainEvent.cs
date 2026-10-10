namespace SupportFlow.BuildingBlocks.Domain;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
