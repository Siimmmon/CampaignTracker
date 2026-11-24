# Campaign Tracker

An Angular 21 and ASP.NET Core 10 application for creating marketing campaigns and viewing their associated events. The development API uses SQLite. The Azure configuration uses Azure SQL or SQL Server.

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

Open [Campaign Tracker](http://127.0.0.1:4200). Create a campaign with an end date after its start date. The app redirects to the saved campaign and displays its ID. Use **Find a campaign** to open it again. In development, Angular proxies `/api` to ASP.NET Core.

### Optional sample events

Once the API has created `api/campaign-tracker.db`, run the following from the project root with the SQLite CLI installed. Enter the printed ID on the **Find a campaign** page. This script can be run again without inserting duplicate sample events.

```powershell
sqlite3 api/campaign-tracker.db '.read database/demo-data.sqlite.sql'
```

The assessment defines campaign creation and event retrieval. The demo seed provides events for trying the read workflow.

## Project structure

| Location | Responsibility |
| --- | --- |
| `api/Controllers/` | HTTP routes and status codes |
| `api/Contracts/` | Request validation and API response shapes |
| `api/Models/` | Campaign and Event entities |
| `api/Data/` | EF Core context, relationship, constraints, and indexes |
| `database/` | Initial SQL Server schema and optional SQLite demo data |
| `web/src/app/campaigns/` | Angular campaign form, lookup, detail, and HttpClient service |
| `infra/` | Bicep for the Linux App Service plan, App Service, and connection string setting |

## API

| Method and path | Behavior |
| --- | --- |
| `POST /api/campaigns` | Creates a campaign and returns `201 Created` with a `Location` header. Invalid input returns `400` with validation details. |
| `GET /api/campaigns/{id}` | Returns the campaign and its events newest first, or `404` for a missing campaign. |

Example POST body

```json
{
  "name": "Spring product launch",
  "startDate": "2026-04-01",
  "endDate": "2026-04-30"
}
```

The API reads ISO dates as UTC midnight and accepts ISO timestamps with offsets. Responses use UTC. The Angular form uses calendar dates, and the detail screen displays event timestamps in UTC.

EF Core reads the requested campaign and its projected event fields in a single no-tracking query. The index begins with `CampaignId` and follows with descending `Timestamp` and `Id`, which also provides a stable order when timestamps match.

### Input handling

The POST endpoint accepts a dedicated DTO containing the name and dates. It trims the name, requires 1–120 characters, and rejects angle brackets and embedded control characters. Both dates are required and the end must be strictly later than the start. The database check constraint also enforces date order.

EF Core parameterizes query values and inserts, so names containing apostrophes and ampersands remain valid data. Angular renders campaign names and event types using text interpolation, which keeps those values out of HTML. The API returns structured validation errors and generic unexpected-error responses.

## Azure deployment

The Bicep template provisions a Linux App Service plan and an HTTPS-only .NET 10 App Service. Apply [the initial SQL schema](database/001-initial-schema.sql) to an empty SQL database first. Give the app identity read access to campaigns and events and insert access to campaigns. The deployment identity needs schema permissions. Configure database network access for App Service.

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
