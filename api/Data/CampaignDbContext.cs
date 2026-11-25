using CampaignTracker.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CampaignTracker.Api.Data;

public sealed class CampaignDbContext(DbContextOptions<CampaignDbContext> options) : DbContext(options)
{
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (Database.IsSqlServer())
            modelBuilder.HasDefaultSchema("dbo");

        var utcConverter = new ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        modelBuilder.Entity<Campaign>(campaign =>
        {
            campaign.ToTable(table => table.HasCheckConstraint("CK_Campaigns_DateRange", "EndDate > StartDate"));
            campaign.Property(item => item.Name).IsRequired().HasMaxLength(Campaign.MaxNameLength);
            campaign.Property(item => item.StartDate).HasConversion(utcConverter);
            campaign.Property(item => item.EndDate).HasConversion(utcConverter);
            campaign.HasMany(item => item.Events)
                .WithOne()
                .HasForeignKey(item => item.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Event>(campaignEvent =>
        {
            campaignEvent.Property(item => item.EventType).IsRequired().HasMaxLength(Event.MaxEventTypeLength);
            campaignEvent.Property(item => item.Timestamp).HasConversion(utcConverter);
            campaignEvent.HasIndex(item => new { item.CampaignId, item.Timestamp, item.Id })
                .IsDescending(false, true, true);
        });
    }
}
