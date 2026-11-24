-- Optional sample data. Start the Development API once to create the tables.
PRAGMA foreign_keys = ON;
BEGIN IMMEDIATE;

INSERT INTO Campaigns (Name, StartDate, EndDate)
SELECT 'Spring product launch (demo)', '2026-04-01 00:00:00', '2026-04-30 00:00:00'
WHERE NOT EXISTS (SELECT 1 FROM Campaigns WHERE Name = 'Spring product launch (demo)');

WITH demo_campaign AS (
    SELECT MIN(Id) AS Id FROM Campaigns WHERE Name = 'Spring product launch (demo)'
), demo_events (EventType, EventTime) AS (
    VALUES ('Email opened', '2026-04-05 10:15:00'),
           ('Link clicked', '2026-04-05 10:17:30'),
           ('Form submitted', '2026-04-05 10:22:10')
)
INSERT INTO Events (EventType, Timestamp, CampaignId)
SELECT demo_events.EventType, demo_events.EventTime, demo_campaign.Id
FROM demo_events CROSS JOIN demo_campaign
WHERE NOT EXISTS (
    SELECT 1 FROM Events
    WHERE Events.CampaignId = demo_campaign.Id
      AND Events.EventType = demo_events.EventType
      AND Events.Timestamp = demo_events.EventTime
);

COMMIT;
SELECT MIN(Id) AS DemoCampaignId FROM Campaigns WHERE Name = 'Spring product launch (demo)';
