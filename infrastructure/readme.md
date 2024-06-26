# Infrastructure readme

## Guidelines

https://github.com/equinor/fusion-infrastructure/blob/main/docs/fusion_standardization_of_azure_resources.md

## Service principals

There are two service principals responsible for doing IAC operations for the FRA solution. These are seggregated into non-production and production environments. 

The SP's are used by pipelines to:
- Create and upload fusion apps to the portal
- Create azure resources in relevant FRA resource groups
- Manage app registrations used by api


**Service principal names**:
- DevOps SP - Fusion resource allocation - production
- DevOps SP - Fusion resource allocation - non-production


### Role assignements

- Reader for subscription S923-Proview
- Contributor for FRA resource groups
- Fusion.Apps.Create
- Fusion database management
- Manage owned applications in graph api