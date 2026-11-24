using CampaignTracker.Api.Contracts;
using CampaignTracker.Api.Data;
using CampaignTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampaignTracker.Api.Controllers;

[ApiController]
[Route("api/campaigns")]
public sealed class CampaignsController(CampaignDbContext database) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CampaignResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var campaign = await database.Campaigns
            .AsNoTracking()
            .AsSingleQuery()
            .Where(campaign => campaign.Id == id)
            .Select(campaign => new CampaignResponse(
                campaign.Id,
                campaign.Name,
                campaign.StartDate,
                campaign.EndDate,
                campaign.Events
                    .OrderByDescending(item => item.Timestamp)
                    .ThenByDescending(item => item.Id)
                    .Select(item => new EventResponse(item.Id, item.EventType, item.Timestamp, item.CampaignId))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        if (campaign is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Campaign not found.");

        return Ok(campaign);
    }

    [HttpPost]
    [RequestSizeLimit(16 * 1024)]
    public async Task<ActionResult<CampaignResponse>> Create(
        CreateCampaignRequest request, CancellationToken cancellationToken)
    {
        var campaign = new Campaign
        {
            Name = request.Name!,
            StartDate = request.StartDate!.Value,
            EndDate = request.EndDate!.Value
        };

        database.Campaigns.Add(campaign);
        await database.SaveChangesAsync(cancellationToken);

        var response = new CampaignResponse(campaign.Id, campaign.Name, campaign.StartDate, campaign.EndDate, []);
        return CreatedAtAction(nameof(GetById), new { id = campaign.Id }, response);
    }
}
