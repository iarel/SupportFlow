using SupportFlow.BuildingBlocks.Domain;
using SupportFlow.Modules.Conversations.Domain;

namespace SupportFlow.Modules.Conversations.Tests.Domain;

public class MessageBodyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void RejectsEmptyText(string text)
    {
        Assert.Throws<DomainException>(() => MessageBody.Create(text));
    }

    [Fact]
    public void AcceptsTextOfMaximumSize()
    {
        var text = new string('a', MessageBody.MaxSizeInBytes);

        Assert.Equal(text, MessageBody.Create(text).Value);
    }

    [Fact]
    public void RejectsTextLargerThanMaximumSize()
    {
        Assert.Throws<DomainException>(() => MessageBody.Create(new string('a', MessageBody.MaxSizeInBytes + 1)));
    }

    [Fact]
    public void MeasuresSizeInUtf8Bytes()
    {
        // 'я' takes two bytes in UTF-8, so this text is within the character count but over the byte limit.
        var text = new string('я', (MessageBody.MaxSizeInBytes / 2) + 1);

        Assert.Throws<DomainException>(() => MessageBody.Create(text));
    }
}
