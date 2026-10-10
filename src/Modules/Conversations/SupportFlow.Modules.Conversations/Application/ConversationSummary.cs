using SupportFlow.Modules.Conversations.Domain;

namespace SupportFlow.Modules.Conversations.Application;

/// <summary>
/// Current state of a conversation for API responses; read without loading the aggregate (architecture.md §6).
/// </summary>
internal sealed record ConversationSummary(
    Guid Id,
    Guid CustomerId,
    string Subject,
    ConversationStatus Status,
    ConversationPriority Priority,
    DateTimeOffset CreatedAt,
    int Version);
