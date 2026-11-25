namespace CampaignTracker.Api.Contracts;

public sealed class CreateCampaignEventRequest
{
    private readonly string? _eventType;
    private readonly DateTime? _timestamp;

    public string? EventType { get => _eventType; init => _eventType = value?.Trim(); }
    public DateTime? Timestamp { get => _timestamp; init => _timestamp = CreateCampaignRequest.ToUtc(value); }
}
