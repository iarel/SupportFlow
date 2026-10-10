namespace SupportFlow.Modules.Conversations.Application;

/// <param name="ConversationId">The created or previously created conversation.</param>
/// <param name="IsCreated">False when the request repeated an already committed one (ADR-0014).</param>
internal sealed record OpenConversationResult(Guid ConversationId, bool IsCreated);
