using SupportFlow.BuildingBlocks.Application;

namespace SupportFlow.Modules.Conversations.Application;

/// <summary>
/// Authorization by ownership (architecture.md §11): a customer sees only their own conversations. Access of staff
/// (FR-005, FR-011) comes with their use cases.
/// </summary>
internal static class ConversationAccess
{
    public static void EnsureCustomerOwns(ConversationSummary conversation, Guid customerId)
    {
        if (conversation.CustomerId != customerId)
        {
            throw new AccessDeniedException("The conversation belongs to another customer.");
        }
    }
}
