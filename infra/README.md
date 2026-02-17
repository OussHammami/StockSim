# Infrastructure (Bicep)

This folder contains Infrastructure as Code for StockSim.

## Scope
`infra/main.bicep` targets the **subscription** scope and creates the environment resource group.

Why subscription scope?
Because resource groups are created at subscription scope (not inside an existing RG).

## Local validation (no Azure subscription required)
Compile Bicep to ARM JSON:
- `az bicep build --file infra/main.bicep --outdir infra/out`

## Deploy (later, when you have a subscription)
- `az deployment sub create --location <region> --template-file infra/main.bicep --parameters infra/params/dev.bicepparam`
