targetScope = 'resourceGroup'

@description('Globally unique App Service name.')
param appName string = 'campaign-${uniqueString(resourceGroup().id)}'

param location string = resourceGroup().location

@secure()
@description('Azure SQL connection string or an App Service Key Vault reference.')
param campaignDbConnectionString string

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: '${appName}-plan'
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
}

resource app 'Microsoft.Web/sites@2024-04-01' = {
  name: appName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ConnectionStrings__CampaignDb'
          value: campaignDbConnectionString
        }
      ]
    }
  }
}

output appUrl string = 'https://${app.properties.defaultHostName}'
output managedIdentityPrincipalId string = app.identity.principalId
