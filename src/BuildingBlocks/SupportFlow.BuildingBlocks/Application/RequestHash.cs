using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace SupportFlow.BuildingBlocks.Application;

/// <summary>
/// SHA-256 of the significant fields of a command, used to detect an idempotency key reused with a different
/// request (ADR-0014).
/// </summary>
public sealed class RequestHash : IEquatable<RequestHash>
{
    public const int SizeInBytes = 32;

    private readonly byte[] _value;

    private RequestHash(byte[] value)
    {
        _value = value;
    }

    /// <summary>
    /// The 32 bytes of the SHA-256, stored as <c>bytea</c>.
    /// </summary>
    public ReadOnlySpan<byte> Value => _value;

    public static bool operator ==(RequestHash? left, RequestHash? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(RequestHash? left, RequestHash? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    /// Hashes the fields in the given order. Each field is length-prefixed, so field boundaries are part of the
    /// hash. Values are hashed as received, before normalization.
    /// </summary>
    public static RequestHash Compute(params ReadOnlySpan<string?> fields)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(int)];

        foreach (var field in fields)
        {
            if (field is null)
            {
                BinaryPrimitives.WriteInt32BigEndian(length, -1);
                hash.AppendData(length);
                continue;
            }

            var bytes = Encoding.UTF8.GetBytes(field);
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        return new RequestHash(hash.GetHashAndReset());
    }

    /// <summary>
    /// Restores a stored hash.
    /// </summary>
    public static RequestHash FromBytes(ReadOnlySpan<byte> value)
    {
        if (value.Length != SizeInBytes)
        {
            throw new ArgumentException($"Request hash must be {SizeInBytes} bytes.", nameof(value));
        }

        return new RequestHash(value.ToArray());
    }

    public bool Equals(RequestHash? other)
    {
        return other is not null && _value.AsSpan().SequenceEqual(other._value);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as RequestHash);
    }

    public override int GetHashCode()
    {
        return BinaryPrimitives.ReadInt32BigEndian(_value);
    }

    /// <summary>
    /// Lowercase hexadecimal SHA-256.
    /// </summary>
    public override string ToString()
    {
        return Convert.ToHexStringLower(_value);
    }
}
