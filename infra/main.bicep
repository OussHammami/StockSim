targetScope = 'subscription'

param rgName string
param location string = 'westeurope'
param namePrefix string = 'stocksim'
param tags object = {
  app: 'stocksim'
  env: 'dev'
}

@description('Environment name (dev/prod/etc.)')
param envName string = 'dev'

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: rgName
  location: location
  tags: tags
}

module env 'modules/env/main.bicep' = {
  name: 'stocksim-env'
  scope: rg
  params: {
    location: location
    tags: tags
    envName: envName
    namePrefix: namePrefix
  }
}

output resourceGroupName string = rg.name
output environmentName string = env.outputs.environmentName
