namespace SupportFlow.Modules.Conversations.Application;

/// <summary>
/// FR-001. <paramref name="CustomerId"/> is the authenticated customer and <paramref name="IdempotencyKey"/>
/// the parsed <c>Idempotency-Key</c> header; the endpoint fills them in.
/// </summary>
internal sealed record OpenConversationCommand(
    Guid CustomerId,
    Guid IdempotencyKey,
    string Subject,
    string FirstMessage);
