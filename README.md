# Fusion App - Resources

The main purpose for the app is to manage personnel in the Equinor Fusion platform.

# Technical

Initial POC / MVP service design:
https://github.com/equinor/fusion/blob/technical/resource-service/services/resources.md

[Role delegation](https://github.com/equinor/fusion/blob/master/docs/technical-design/resources/role-delegation.md)

## Infrastructure

The app will manage it's own infrastructure. This includes separate Azure AD App Registration.
This makes the app transferable to other teams.

### Azure AD

> For now the fusion ad app is backing the resources api.

Test app: [GUID]
Production app: [GUID]

## Functions

### Scheduled report function

This functions send a weekly report to task- and resource-owners.

#### Flow

- The time triggered function (`ScheduledReportTimerTriggerFunction.cs`)
  run once every week (every sunday at 6 AM UTC). The time triggered unction call the LineOrg API to get all resourceOwners.
- Individual recipients are sent to a queue on Azure Servicebus.
- The content builder function (`ScheduledReportContentBuilderFunction.cs`) is triggered by the queue.
  The content builder function generate an adaptive card specific to each
  recipient and their respective department. 
- The email content is sent to the Core Notifications API which send the email to the recipient.

```mermaid
sequenceDiagram
  Time triggered function ->>+ Resource API: Get Departments
  Resource API ->>+ Time triggered function: Departments
  Time triggered function ->>+ LineOrg API: Get recipients for department
  LineOrg API ->>+ Time triggered function: Recipients
  Time triggered function ->>+ Servicebus queue: Recipient
  Servicebus queue ->>+ Content builder function: Recipient
  Content builder function ->>+ Core Notifications API: Content for recipient
  Core Notifications API ->>+ Content builder function: Result
```
