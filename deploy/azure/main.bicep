targetScope = 'resourceGroup'
@description('Proposal only: validate in an isolated subscription. Application rollout is not included.')
param location string = 'uksouth'
param prefix string = 'riptreasury'
@secure()
param postgresPassword string
param postgresAdministrator string = 'treasuryadmin'
var suffix = uniqueString(resourceGroup().id)
resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: '${prefix}${suffix}'
  location: location
  sku: { name: 'Basic' }
  properties: { adminUserEnabled: false }
}
resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${prefix}-logs'
  location: location
  properties: { sku: { name: 'PerGB2018' }
retentionInDays: 30 }
}
resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${prefix}-insights'
  location: location
  kind: 'web'
  properties: { Application_Type: 'web'
WorkspaceResourceId: logs.id }
}
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${prefix}-runtime'
  location: location
}
resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'rt-${suffix}'
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: { family: 'A'
name: 'standard' }
    enableRbacAuthorization: true
    enablePurgeProtection: true
    softDeleteRetentionInDays: 90
  }
}
resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${prefix}-environment'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}
resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: '${prefix}-${suffix}'
  location: location
  sku: { name: 'Standard_B1ms'
tier: 'Burstable' }
  properties: {
    version: '16'
    administratorLogin: postgresAdministrator
    administratorLoginPassword: postgresPassword
    storage: { storageSizeGB: 32 }
    backup: { backupRetentionDays: 7
geoRedundantBackup: 'Disabled' }
    network: { publicNetworkAccess: 'Disabled' }
  }
}
// Networking/private DNS and role assignments must be designed before use.
// API/worker Container App revisions are gated until PostgreSQL/bank/auth adapters are implemented and tested.
resource frontend 'Microsoft.Web/staticSites@2023-12-01' = {
  name: '${prefix}-web'
  location: location
  sku: { name: 'Free'
tier: 'Free' }
  properties: { allowConfigFileUpdates: true }
}
output registryName string = registry.name
output environmentId string = environment.id
output runtimeIdentityId string = identity.id
output keyVaultUri string = vault.properties.vaultUri
output insightsConnectionString string = insights.properties.ConnectionString

