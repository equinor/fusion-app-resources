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
