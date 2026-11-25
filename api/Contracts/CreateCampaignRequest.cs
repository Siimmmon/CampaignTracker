using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CampaignTracker.Api.Models;

namespace CampaignTracker.Api.Contracts;

public sealed class CreateCampaignRequest : IValidatableObject
{
    public const int MaxEvents = 20;

    private readonly string? _name;
    private readonly DateTime? _startDate;

    public string? Name { get => _name; init => _name = value?.Trim(); }
    public DateTime? StartDate { get => _startDate; init => _startDate = ToUtc(value); }
    public JsonElement EndDate { get; init; }
    public IReadOnlyList<CreateCampaignEventRequest?>? Events { get; init; }

    internal DateTime? EndDateUtc => EndDate.ValueKind == JsonValueKind.String && EndDate.TryGetDateTime(out var date)
        ? ToUtc(date)
        : null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Name))
            yield return new ValidationResult("Enter a campaign name.", ["name"]);
        else if (Name.Length > Campaign.MaxNameLength)
            yield return new ValidationResult($"Use {Campaign.MaxNameLength} characters or fewer.", ["name"]);
        else if (ContainsMarkupOrControls(Name))
            yield return new ValidationResult("Use plain text without angle brackets or control characters.", ["name"]);

        if (StartDate is null)
            yield return new ValidationResult("Enter a start date.", ["startDate"]);

        var endDate = EndDateUtc;
        if (endDate is null)
        {
            var message = EndDate.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? "Enter an end date."
                : "Enter a valid ISO end date or timestamp.";
            yield return new ValidationResult(message, ["endDate"]);
        }
        else if (StartDate is not null && endDate <= StartDate)
            yield return new ValidationResult("End date must be after the start date.", ["endDate"]);

        if (Events is null)
            yield break;

        if (Events.Count > MaxEvents)
        {
            yield return new ValidationResult($"Add no more than {MaxEvents} events.", ["events"]);
            yield break;
        }

        // An ISO calendar date includes the entire UTC day for event validation.
        var endIncludesFullDay = endDate.HasValue && EndDate.GetString()!.Length == 10;
        for (var index = 0; index < Events.Count; index++)
        {
            var entry = Events[index];
            var path = $"events[{index}]";
            var label = $"Event {index + 1}";

            if (entry is null)
            {
                yield return new ValidationResult($"{label} must include a type and timestamp.", [path]);
                continue;
            }

            if (string.IsNullOrEmpty(entry.EventType))
                yield return new ValidationResult($"{label} needs an event type.", [$"{path}.eventType"]);
            else if (entry.EventType.Length > Event.MaxEventTypeLength)
                yield return new ValidationResult($"{label} type must use {Event.MaxEventTypeLength} characters or fewer.", [$"{path}.eventType"]);
            else if (ContainsMarkupOrControls(entry.EventType))
                yield return new ValidationResult($"{label} type must use plain text without angle brackets or control characters.", [$"{path}.eventType"]);

            if (entry.Timestamp is not DateTime timestamp)
                yield return new ValidationResult($"{label} needs a timestamp.", [$"{path}.timestamp"]);
            else if (StartDate is DateTime start && endDate is DateTime end && end > start &&
                (timestamp < start || (endIncludesFullDay ? timestamp.Date > end.Date : timestamp > end)))
                yield return new ValidationResult($"{label} timestamp must be within the campaign schedule.", [$"{path}.timestamp"]);
        }
    }

    private static bool ContainsMarkupOrControls(string value) =>
        value.Contains('<') || value.Contains('>') || value.Any(char.IsControl);

    internal static DateTime? ToUtc(DateTime? date) => date?.Kind switch
    {
        DateTimeKind.Local => date.Value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(date.Value, DateTimeKind.Utc),
        _ => date
    };
}
