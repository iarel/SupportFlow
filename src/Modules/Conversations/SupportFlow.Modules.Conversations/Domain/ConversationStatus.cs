namespace SupportFlow.Modules.Conversations.Domain;

internal enum ConversationStatus
{
    New,
    InProgress,
    WaitingOnCustomer,
    Resolved,
    Closed,
}
