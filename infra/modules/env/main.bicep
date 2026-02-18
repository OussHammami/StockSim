targetScope = 'resourceGroup'

@description('Azure region')
param location string

@description('Common tags')
param tags object

@description('Environment name (dev/prod/etc.)')
param envName string

@description('Short prefix used in resource names')
param namePrefix string = 'stocksim'

@description('Log Analytics retention in days')
param logRetentionInDays int = 30

var suffix = uniqueString(resourceGroup().id)
var baseName = toLower('${namePrefix}-${envName}-${suffix}')

var logWorkspaceName = '${baseName}-la'
var acaEnvName = '${baseName}-acaenv'

resource log 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logWorkspaceName
  location: location
  tags: tags
  properties: {
    retentionInDays: logRetentionInDays
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource acaEnv 'Microsoft.App/managedEnvironments@2025-01-01' = {
  name: acaEnvName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: log.properties.customerId
        sharedKey: log.listKeys().primarySharedKey
      }
    }
    // Keep this minimal (no VNET yet)
    vnetConfiguration: {}
  }
}

output logAnalyticsWorkspaceId string = log.id
output containerAppsEnvironmentId string = acaEnv.id
output containerAppsEnvironmentName string = acaEnv.name
output environmentName string = envName
