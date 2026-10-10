using SupportFlow.Modules.Conversations.Domain;

namespace SupportFlow.Modules.Conversations.Application;

/// <summary>
/// A message as shown in the conversation history. The author id is not exposed to customers.
/// </summary>
internal sealed record MessageView(Guid Id, long Seq, AuthorType AuthorType, string Body, DateTimeOffset CreatedAt);
