# Role Delegation

The administrative responsibilities can be delegated by the CR to specific persons. These persons will then have the access to execute the same operations, except for delegate access to new persons.

The delegated access have a time limit of max 1 year and must re-certified to extend the access. 

Delegation is classified in `internal` and `external` and `type`. The classification determines which side of the relation the person has access. `Internal` lets the user take on the company responsibility while the `external` grants access to the contractor actions - like manage personnel, create requests etc.

Valid `types` are:
- `CR`

**Access overview**  
The internal CR have the ability to delegate access to both external and internal classification. This is mostly for convenience, in case the external side have issues with accounts etc.

The internal classification cannot be delegated to accounts of type `external`. This is to protect against accidental delegations.

Only the actual CR role can delegate access.

**Expiration**
The roles have a valid to date, when this is reached, the access will no longer be in effect. This is affects the access to the org service as well. 

When a delegation is expired, it must be re-certified or removed/deleted.

## Technical design
The delegated roles are stored and managed in the resources service. This holds a representation of the role type, assigned person and relevant contract/project. 

When a role delegation is created, a fusion role assignment `Fusion.Contract.Read` with the scope `Contract: [org contract id]` is pushed to the roles service. The assignment is created with expiration date. This role will give access to read contract info in the org service.

Expired roles are physically removed from the roles service, so when a expired role is re-certified, it must be recreated in the roles service.

