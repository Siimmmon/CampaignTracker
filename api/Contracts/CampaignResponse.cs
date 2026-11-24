namespace CampaignTracker.Api.Contracts;

public sealed record CampaignResponse(
    int Id, string Name, DateTime StartDate, DateTime EndDate, IReadOnlyList<EventResponse> Events);

public sealed record EventResponse(int Id, string EventType, DateTime Timestamp, int CampaignId);
