namespace CampaignTracker.Api.Models;

public sealed class Campaign
{
    public const int MaxNameLength = 120;

    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Event> Events { get; } = [];
}
