namespace BuildingBlocks.IntegrationEvents;

/// <summary>
/// Base type for every message that travels across services on the bus.
/// Carries a correlation id so a single checkout flow can be traced
/// end-to-end across Order -> Inventory -> Payment -> Notification.
/// </summary>
public abstract record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    public Guid CorrelationId { get; init; }
}
