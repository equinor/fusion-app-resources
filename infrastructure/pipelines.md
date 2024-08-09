# FRA Pipelines

An overview of pipelines in FRA.

## Pipeline structure

Pipelines are added to the devops project, (Fusion Resource Allocation)[https://statoil-proview.visualstudio.com/Fusion%20Resource%20Allocation/_build?view=folders]. 
They are grouped into different folders, as described in seperate headings below.

### Frontend

Pipelines related to ci/cd for fusion apps. The pipelines runs to published completed releases of the apps to ci->production and for individual prs. These pr apps will be cleaned up automatically by the AKS infra.

### Backend

Apis and function app deployment. All non-fusion-app prs should be here, where there are "customers".

Naming convention: 

- API - [Domain]
- FUNC - [Domain]


### Maintenance

Pipelines to do mainly scheduled work. E.g. key rotation, cleanup, monitoring etc.

Naming convention: 
- OPR - [Domain] - [Job]
- OPR - [Job]
- MON - [?]
- BACKUP - [Name | Data]  // e.g. BACKUP - App Registrations

### Infrastructure

Pipelines that sets up baseline infra, e.g. shared resources like app insights, key vaults for environments etc.
These should not be triggered frequently due to changes, however they could perhaps be triggered scheduled, just to ensure they are working when needed. This would require them to be designed to be idempotent.


## Service Connection


## Guide / Getting started

### Adding new pipeline

1. Identify the correct folder
1. Add new pipeline
1. Select repository. If the repository is not located, the github connection might need to be updated to include a new repository

> When updating the github connection, try to align this with the person that did it last. If the person updating it does not have access to the already added repos, they will be removed, which will cause existing repos to not run.

1. Save (without running)
1. Update the name of the pipeline immediatly (... -> Rename). The default name is just the name of the repo, which is not good. Utilize the naming convention for a PROPER name.
