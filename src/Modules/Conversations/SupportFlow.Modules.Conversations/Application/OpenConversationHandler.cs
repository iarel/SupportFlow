using SupportFlow.BuildingBlocks.Application;
using SupportFlow.Modules.Conversations.Domain;
using SupportFlow.Modules.SupportOrganization.Contracts;

namespace SupportFlow.Modules.Conversations.Application;

internal sealed class OpenConversationHandler(
    IConversationRepository conversations,
    IMessageRepository messages,
    IIdempotencyStore idempotencyKeys,
    IIntegrationEventOutbox outbox,
    ITeamQueries teams,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public const string Operation = "OpenConversation";

    /// <summary>
    /// Requirements §4.7: at most 10 new conversations per customer per day.
    /// </summary>
    public const int DailyConversationLimit = 10;

    /// <summary>
    /// Rolling window of the daily limit (ADR-0012).
    /// </summary>
    public static readonly TimeSpan DailyLimitWindow = TimeSpan.FromDays(1);

    public async Task<OpenConversationResult> HandleAsync(
        OpenConversationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var scope = new IdempotencyScope(Operation, command.CustomerId, command.IdempotencyKey);
        var requestHash = RequestHash.Compute(command.Subject, command.FirstMessage);

        // A repeated request returns the existing conversation before any limit is checked (ADR-0014).
        var existing = await idempotencyKeys.FindAsync(scope, cancellationToken);

        if (existing is not null)
        {
            return new OpenConversationResult(existing.ResolveReplay(requestHash), IsCreated: false);
        }

        var firstMessage = MessageBody.Create(command.FirstMessage);
        var teamId = await teams.GetDefaultTeamIdAsync(cancellationToken);
        var conversationId = Guid.CreateVersion7(timeProvider.GetUtcNow());

        try
        {
            await unitOfWork.ExecuteAsync(
                async ct =>
                {
                    // First statement of the transaction: a concurrent repeat waits here instead of hitting
                    // the daily limit. The key is always locked before the customer (ADR-0014).
                    await idempotencyKeys.RegisterAsync(scope, requestHash, conversationId, ct);
                    await conversations.LockCustomerConversationCreationAsync(command.CustomerId, ct);

                    var now = timeProvider.GetUtcNow();

                    var openedLastDay = await conversations.CountOpenedByCustomerSinceAsync(
                        command.CustomerId,
                        now - DailyLimitWindow,
                        ct);

                    if (openedLastDay.Count >= DailyConversationLimit)
                    {
                        // A new conversation is possible once the earliest one leaves the window.
                        var retryAfter = openedLastDay.EarliestCreatedAt is { } earliest
                            ? earliest + DailyLimitWindow - now
                            : DailyLimitWindow;

                        throw new DailyConversationLimitExceededException(
                            $"Customer {command.CustomerId} opened {openedLastDay.Count} conversations in the last 24 hours.",
                            retryAfter);
                    }

                    var (conversation, message) = Conversation.Open(
                        conversationId,
                        command.CustomerId,
                        teamId,
                        command.Subject,
                        firstMessage,
                        now);

                    conversations.Add(conversation);
                    messages.Add(message);
                    IntegrationEvents.Publish(conversation, outbox);

                    return conversation.Id;
                },
                cancellationToken);

            return new OpenConversationResult(conversationId, IsCreated: true);
        }
        catch (DuplicateIdempotencyKeyException)
        {
            // A concurrent request with the same key committed first (ADR-0014).
            var winner = await idempotencyKeys.FindAsync(scope, cancellationToken)
                ?? throw new InvalidOperationException(
                    "Idempotency key conflict reported, but the key is not stored.");

            return new OpenConversationResult(winner.ResolveReplay(requestHash), IsCreated: false);
        }
    }
}
