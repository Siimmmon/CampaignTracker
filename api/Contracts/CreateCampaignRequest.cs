using System.ComponentModel.DataAnnotations;
using CampaignTracker.Api.Models;

namespace CampaignTracker.Api.Contracts;

public sealed class CreateCampaignRequest : IValidatableObject
{
    private readonly string? _name;
    private readonly DateTime? _startDate;
    private readonly DateTime? _endDate;

    public string? Name { get => _name; init => _name = value?.Trim(); }
    public DateTime? StartDate { get => _startDate; init => _startDate = ToUtc(value); }
    public DateTime? EndDate { get => _endDate; init => _endDate = ToUtc(value); }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Name))
            yield return new ValidationResult("Enter a campaign name.", ["name"]);
        else if (Name.Length > Campaign.MaxNameLength)
            yield return new ValidationResult($"Use {Campaign.MaxNameLength} characters or fewer.", ["name"]);
        else if (Name.Contains('<') || Name.Contains('>') || Name.Any(char.IsControl))
            yield return new ValidationResult("Use plain text without angle brackets or control characters.", ["name"]);

        if (StartDate is null)
            yield return new ValidationResult("Enter a start date.", ["startDate"]);

        if (EndDate is null)
            yield return new ValidationResult("Enter an end date.", ["endDate"]);
        else if (StartDate is not null && EndDate <= StartDate)
            yield return new ValidationResult("End date must be after the start date.", ["endDate"]);
    }

    private static DateTime? ToUtc(DateTime? date) => date?.Kind switch
    {
        DateTimeKind.Local => date.Value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(date.Value, DateTimeKind.Utc),
        _ => date
    };
}
