targetScope = 'resourceGroup'

@description('Azure region')
param location string

@description('Common tags')
param tags object

@description('Environment name (dev/prod/etc.)')
param envName string

// No resources yet. This module exists to establish structure and scoping.

output environmentName string = envName
output deployedLocation string = location
