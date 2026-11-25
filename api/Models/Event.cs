namespace CampaignTracker.Api.Models;

public sealed class Event
{
    public const int MaxEventTypeLength = 80;

    public int Id { get; set; }
    public required string EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public int CampaignId { get; set; }
}
