using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CampaignTracker.Api.Data;

public sealed class CampaignDbContextFactory : IDesignTimeDbContextFactory<CampaignDbContext>
{
    public CampaignDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CampaignDb");
        var options = new DbContextOptionsBuilder<CampaignDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new CampaignDbContext(options);
    }
}
