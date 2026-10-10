namespace SupportFlow.BuildingBlocks.Application;

/// <summary>
/// Scope of an idempotency key: the key is unique per caller and operation (ADR-0014).
/// </summary>
public sealed record IdempotencyScope(string Operation, Guid CallerId, Guid Key);
