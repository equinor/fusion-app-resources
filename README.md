# Fusion App - Resources

The main purpose for the app is to manage personnel in the Equinor Fusion platform. 

First features consists of:
- Allocate contract
- Define personnel relevant for contracts
 - Display Azure AD account status for defined persons
- Create requests that will populate org chart positions when approved

## Manage contract personnel

[Documentation](docs/contract-management.md)

Contracts will be allocated on demand, by contract number, retrieved from common lib/SAP.

When the contract is allocated, company can assign contractor comp rep & contract responsible. These will be the first positions added to the contract.

When external reps have been added, they will have access to start defining personnel. 
This is done by entering their emails, using their own company email, to use the Azure AD guest accounts.
The personnel added will be verified against Azure AD, giving the reps an overview of whom needs to be invited as affiliate access accounts.

After the personnel is defined, they can be used when creating requests. The external reps can create request to create positions and assign persons to these. When a person is assign a position in a contract, this person will start to get access to different systems automatically.

## Role delegation
[Documentation](docs/role-delegation.md)

Access given to the internal and external CR can be delegated to specific persons. This delegation is time-limited and must be recertified, with a max time span of 1 year.

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

