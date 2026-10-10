namespace SupportFlow.BuildingBlocks.Outbox;

/// <summary>
/// Stable names of a module's integration events, stored in the outbox instead of CLR type names, so renaming a
/// record does not break events already in the outbox (ADR-0015). Only registered types can be written.
/// </summary>
public sealed class IntegrationEventTypes
{
    private readonly Dictionary<Type, string> _names = [];
    private readonly HashSet<string> _usedNames = [];

    public IntegrationEventTypes Add<TEvent>(string name)
        where TEvent : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!_usedNames.Add(name) || !_names.TryAdd(typeof(TEvent), name))
        {
            throw new ArgumentException($"Integration event {typeof(TEvent).Name} or name '{name}' is already registered.");
        }

        return this;
    }

    public string GetName(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return _names.TryGetValue(eventType, out var name)
            ? name
            : throw new InvalidOperationException(
                $"{eventType.Name} is not a registered integration event of this module.");
    }
}
