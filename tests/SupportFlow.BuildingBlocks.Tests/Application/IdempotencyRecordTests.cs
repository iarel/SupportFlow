using SupportFlow.BuildingBlocks.Application;

namespace SupportFlow.BuildingBlocks.Tests.Application;

public class IdempotencyRecordTests
{
    private static readonly Guid _resourceId = Guid.NewGuid();

    [Fact]
    public void ResolvesReplayWithSameRequestToStoredResource()
    {
        var record = new IdempotencyRecord(RequestHash.Compute("request"), _resourceId);

        Assert.Equal(_resourceId, record.ResolveReplay(RequestHash.Compute("request")));
    }

    [Fact]
    public void RejectsReplayWithDifferentRequest()
    {
        var record = new IdempotencyRecord(RequestHash.Compute("request"), _resourceId);

        Assert.Throws<IdempotencyKeyReusedException>(() => record.ResolveReplay(RequestHash.Compute("other")));
    }
}
