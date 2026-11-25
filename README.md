# Campaign Tracker

An Angular 21 and ASP.NET Core 10 application for creating marketing campaigns and viewing their associated events. The development API uses SQLite. The Azure configuration uses Azure SQL or SQL Server.

## Technical assessment

- **Part 1 — Mini Practical.** [Run locally](#run-locally) · [Campaign API and security](#api) · [Angular campaign code](web/src/app/campaigns/) · [EF Core migration](#apply-the-ef-core-migration) · [Azure Bicep](#azure-deployment)
- **Part 2 — AI Code Review & Workflow.** [Review findings](assessment/part-2/README.md) · [Final C# rewrite](assessment/part-2/OrderQueries.cs) · [Prompt, raw AI response, and three changes](assessment/part-2/AI-WORKFLOW.md)
- **Part 3 — AI Context Configuration.** [AGENTS.md](AGENTS.md) defines the project conventions for AI coding agents.

## Run locally

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and [Node.js 22.12 or newer in the 22.x line](https://nodejs.org/en/download). Run these commands from the project root in two terminals.

**Terminal 1**

```powershell
dotnet run --project api/CampaignTracker.Api.csproj --launch-profile local
```

The API listens on `http://127.0.0.1:5080` and creates the development SQLite database on first start.

**Terminal 2**

```powershell
cd web
npm ci
npm start
```

Open [Campaign Tracker](http://127.0.0.1:4200). Create a campaign with an end date after its start date. Use **Add event** below the dates to enter an event type and a UTC timestamp. You can save up to 20 events with the campaign. The app redirects to the saved campaign and displays its ID and events. Use **Find a campaign** to open it again. In development, Angular proxies `/api` to ASP.NET Core.

## Project structure

The ASP.NET Core API groups controllers, contracts, entities, and EF Core configuration by responsibility so each part of a request is easy to locate. The Angular app groups the form, lookup, detail view, and HTTP service together so reviewers can follow the campaign workflow in one folder. Azure infrastructure and the Part 2 review each have their own folder for easy reference.

| Location | Responsibility |
| --- | --- |
| `api/Controllers/` | HTTP routes and status codes |
| `api/Contracts/` | Request validation and API response shapes |
| `api/Models/` | Campaign and Event entities |
| `api/Data/` | EF Core context, relationship, constraints, and indexes |
| `api/Data/Migrations/` | SQL Server initial migration and EF Core model snapshot |
| `web/src/app/campaigns/` | Angular campaign form, lookup, detail, and HttpClient service |
| `infra/` | Bicep for the Linux App Service plan, App Service, and connection string setting |
| `assessment/part-2/` | Order query review, final rewrite, and AI workflow evidence |
| `AGENTS.md` | Repository-wide instructions for AI coding agents |

## API

| Method and path | Behavior |
| --- | --- |
| `POST /api/campaigns` | Creates a campaign and its optional events in one save. Returns `201 Created` with a `Location` header. Invalid input returns `400` with validation details. |
| `GET /api/campaigns/{id}` | Returns the campaign and its events newest first, or `404` for a missing campaign. |

Example POST body

```json
{
  "name": "Spring product launch",
  "startDate": "2026-04-01",
  "endDate": "2026-04-30",
  "events": [
    { "eventType": "Email opened", "timestamp": "2026-04-05T10:15:00Z" }
  ]
}
```

The API reads ISO dates as UTC midnight and accepts ISO timestamps with offsets. Responses use UTC. Events must fall within the campaign schedule. A calendar end date includes that entire UTC day for event validation, while an end timestamp sets the exact cutoff. The Angular form uses calendar dates, and the detail screen displays event timestamps in UTC.

EF Core reads the requested campaign and its projected event fields in a single no-tracking query. The index begins with `CampaignId` and follows with descending `Timestamp` and `Id`, which also provides a stable order when timestamps match.

### Security - Input handling

- **Input validation and sanitization.** The POST trims campaign names and event types, rejects angle brackets and embedded control characters, and limits names to 120 characters and event types to 80. Both campaign dates are required, the end must be strictly later than the start, and each event timestamp must fall within that schedule. The database check constraint also enforces campaign date order. [api/Contracts/CreateCampaignRequest.cs](api/Contracts/CreateCampaignRequest.cs#L25) · [api/Contracts/CreateCampaignEventRequest.cs](api/Contracts/CreateCampaignEventRequest.cs#L8)
- **SQL injection protection.** EF Core sends insert values as SQL parameters. Names containing apostrophes and ampersands are handled as ordinary data. [api/Controllers/CampaignsController.cs](api/Controllers/CampaignsController.cs#L53)
- **Cross-site scripting (XSS) protection.** Angular renders campaign names and event types as escaped text through template interpolation. [web/src/app/campaigns/detail/campaign-detail.component.html](web/src/app/campaigns/detail/campaign-detail.component.html#L24)

The API returns structured validation errors and generic unexpected-error responses.

## Azure deployment

The Bicep template provisions a Linux App Service plan and an HTTPS-only .NET 10 App Service. The parameter file supplies the secure database connection string setting. [infra/main.bicep](infra/main.bicep) · [infra/main.bicepparam](infra/main.bicepparam)

Give the app identity `SELECT` and `INSERT` permissions on `dbo.Campaigns` and `dbo.Events`. Configure database network access for App Service.

### Apply the EF Core migration

For a new deployment, set `ConnectionStrings__CampaignDb` in your terminal to the connection string for an empty Azure SQL or SQL Server database. Use a deployment identity with schema permissions. Run these commands from the project root.

```powershell
dotnet tool restore
dotnet ef database update --project api/CampaignTracker.Api.csproj
```

On the first update to an empty SQL database, EF Core runs `InitialCreate.Up()` to create the `Campaigns` and `Events` tables, their foreign key, the campaign date check constraint, and the event lookup index. EF Core records the migration in `__EFMigrationsHistory`. Later runs of `dotnet ef database update` skip migrations already recorded there. [InitialCreate migration](api/Data/Migrations/20261125042813_InitialCreate.cs#L12)

For local SQLite development, the API calls `EnsureCreatedAsync()` on startup to create the tables when the database is initialized. [SQLite startup code](api/Program.cs#L27)

If a SQL Server database already uses the schema from the earlier standalone script, retain its data and have the deployment owner verify the schema and record the initial migration in EF's history before applying future migrations.

### Build the deployment package

Run these PowerShell commands from the project root. The API serves Angular and the API on the same origin, including direct links to campaign detail pages.

```powershell
npm --prefix web ci
npm --prefix web run build
dotnet publish api/CampaignTracker.Api.csproj -c Release -o publish
New-Item -ItemType Directory -Path publish/wwwroot -Force | Out-Null
Copy-Item -Path 'web/dist/campaign-tracker/browser/*' -Destination 'publish/wwwroot' -Recurse -Force
Compress-Archive -Path 'publish/*' -DestinationPath 'campaign-tracker-deploy.zip' -Force
```

### Provision and publish

Set `CAMPAIGN_DB_CONNECTION_STRING` in the deployment environment to a connection string or App Service Key Vault reference. The Bicep parameter is secure and is written to the `ConnectionStrings__CampaignDb` app setting. When using a Key Vault reference, grant the App Service system-assigned identity permission to read its secret. For a managed-identity SQL connection, grant that identity the SQL permissions described above before publishing.

```powershell
$resourceGroup = 'your-resource-group'
az bicep build --file infra/main.bicep
$appUrl = az deployment group create --name campaign-tracker --resource-group $resourceGroup `
  --parameters infra/main.bicepparam --query properties.outputs.appUrl.value --output tsv
$appName = ([uri]$appUrl).Host.Split('.')[0]
az webapp deploy --resource-group $resourceGroup --name $appName `
  --src-path campaign-tracker-deploy.zip --type zip
```

The assessment build uses unauthenticated API endpoints. Restrict App Service access to intended testers or configure authentication before exposing the app publicly.
