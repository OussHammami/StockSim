using '../main.bicep'

param rgName = 'stocksim-dev'
param location = 'westeurope'
param envName = 'dev'
param tags = {
  app: 'stocksim'
  env: 'dev'
}
