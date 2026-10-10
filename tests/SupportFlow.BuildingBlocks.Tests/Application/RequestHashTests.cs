using SupportFlow.BuildingBlocks.Application;

namespace SupportFlow.BuildingBlocks.Tests.Application;

public class RequestHashTests
{
    [Fact]
    public void IsEqualForSameFields()
    {
        Assert.Equal(RequestHash.Compute("subject", "body"), RequestHash.Compute("subject", "body"));
    }

    [Fact]
    public void DependsOnFieldBoundaries()
    {
        Assert.NotEqual(RequestHash.Compute("ab", "c"), RequestHash.Compute("a", "bc"));
    }

    [Fact]
    public void DependsOnFieldOrder()
    {
        Assert.NotEqual(RequestHash.Compute("a", "b"), RequestHash.Compute("b", "a"));
    }

    [Fact]
    public void DistinguishesNullFromEmptyField()
    {
        Assert.NotEqual(RequestHash.Compute(null, "b"), RequestHash.Compute(string.Empty, "b"));
    }

    [Fact]
    public void DoesNotNormalizeWhitespace()
    {
        Assert.NotEqual(RequestHash.Compute("subject"), RequestHash.Compute(" subject "));
    }

    [Fact]
    public void IsSha256OfLengthPrefixedFields()
    {
        // SHA-256 of a single field: 4-byte big-endian length 0 followed by no data.
        var hash = RequestHash.Compute(string.Empty);

        Assert.Equal(RequestHash.SizeInBytes, hash.Value.Length);
        Assert.Equal("df3f619804a92fdb4057192dc43dd748ea778adc52bc498ce80524c014b81119", hash.ToString());
    }

    [Fact]
    public void RestoresStoredBytes()
    {
        var hash = RequestHash.Compute("subject", "body");

        Assert.Equal(hash, RequestHash.FromBytes(hash.Value));
    }

    [Fact]
    public void RejectsStoredValueOfWrongLength()
    {
        Assert.Throws<ArgumentException>(() => RequestHash.FromBytes(new byte[RequestHash.SizeInBytes - 1]));
    }
}
