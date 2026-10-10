using System.Text;
using SupportFlow.BuildingBlocks.Domain;

namespace SupportFlow.Modules.Conversations.Domain;

internal sealed record MessageBody
{
    /// <summary>
    /// Maximum message size, requirements §4.7.
    /// </summary>
    public const int MaxSizeInBytes = 10 * 1024;

    private MessageBody(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static MessageBody Create(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainException("Message body must not be empty.");
        }

        if (Encoding.UTF8.GetByteCount(text) > MaxSizeInBytes)
        {
            throw new DomainException($"Message body must not exceed {MaxSizeInBytes} bytes.");
        }

        return new MessageBody(text);
    }
}
