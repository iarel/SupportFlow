using System.Diagnostics.CodeAnalysis;

namespace SupportFlow.BuildingBlocks.Outbox;

/// <summary>
/// Stable names of a module's integration events, stored in the outbox instead of CLR type names, so renaming a
/// record does not break events already in the outbox (ADR-0015). Only registered types can be written.
/// </summary>
public sealed class IntegrationEventTypes
{
    private readonly Dictionary<Type, string> _names = [];
    private readonly Dictionary<string, Type> _types = [];

    public IntegrationEventTypes Add<TEvent>(string name)
        where TEvent : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_names.ContainsKey(typeof(TEvent)) || !_types.TryAdd(name, typeof(TEvent)))
        {
            throw new ArgumentException($"Integration event {typeof(TEvent).Name} or name '{name}' is already registered.");
        }

        _names.Add(typeof(TEvent), name);
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

    public bool TryGetType(string name, [NotNullWhen(true)] out Type? eventType)
    {
        return _types.TryGetValue(name, out eventType);
    }
}
