# Contract Management

## Definitions
![#6f30a0](https://placehold.it/15/6f30a0/000000?text=+) Company</br>
![#d66dd3](https://placehold.it/15/d66dd3/000000?text=+) Contractor</br>
![#ff0000](https://placehold.it/15/ff0000/000000?text=+) Fusion system</br>

#### Roles
*These roles each have the possibility for a counter part on the contractor side, however the descriptions below apply only to the company side, as defined in ARIS. Contractors can have their own definitions.*

The persons assigned to the roles will by default have the ability to perform actions like manage personnel and approve/reject request.

> **Company Representative (CR)** ![#6f30a0](https://placehold.it/15/6f30a0/000000?text=+)![#d66dd3](https://placehold.it/15/d66dd3/000000?text=+)
>
> This role acts as the company's single point of contact towards the contractor/supplier when the agreement has been awarded. Agreement is a collective term which covers contracts, amendments, purchase orders and framework agreements.
> 
> This role acts as the leader of the agreement team.

> **Procurement Responsible (PR)** ![#6f30a0](https://placehold.it/15/6f30a0/000000?text=+)![#d66dd3](https://placehold.it/15/d66dd3/000000?text=+)
> 
> The responsible person for planning, coordinating and executing the procurement process, and with a delegated authority to commit the company towards suppliers, ensuring right quality and commercial terms.

#### Contracts
Contracts in this context, are POs of a certain scale, originating in SAP and resolved from common library. 

Common library will provide list available contracts for a specific project (project master), and provides the contract number, contract name and the company name.

#### Affiliate account
A guest account in the company Azure AD. This means the user can sign in to the company systems using their own e-mail. To get this account; **1.** the e-mail must be invited; **2.** the user receives a mail with a link; **3** the user has to follow the link and agree to the company terms.

# Request flow
<p align="center">  <img src="https://github.com/equinor/fusion-app-resources/blob/master/docs/images/contracts-request-flow.svg">  </p>

## The overall process

<p align="center">  <img src="https://github.com/equinor/fusion-app-resources/blob/master/docs/images/contracts-general-process.svg">  </p>

>  ### 1. Allocation
> The contract is allocated by a company employ with correct permission. This permission is granted to persons with procurement positions in the selected project, or super-users with said permission for all projects.
> Contracts are listed from common library for the selected project. 
>
> **If the contract does not appear, it must be added to common library. This can be done by raising a Service Now ticket**

> ### 2. Manage Personnel
> ![#d66dd3](https://placehold.it/15/d66dd3/000000?text=+) CR/PR can define personnel that will be available for the request process. 
> This step is isolated from the request process, to give a clear view of users that is involved in the contract, and a workspace to manage general user info, like relevant disciplines, davinci code, linked in profile etc.
>
> Once a user is added to the personnel list, the system will do a check to see if that user has an affiliate account. This is indicated with a **grey**/**yellow**/**green** icon. 
> 
> ![#a6a6a6](https://placehold.it/15/cacaca/000000?text=+) **Grey** indicates the user does not have an account.</br>
> ![#f0b913](https://placehold.it/15/f0b913/000000?text=+) **Yellow** means the user has **not** accepted the invitation yet. That results in the user not being able to sign in.</br>
> ![#1fcf00](https://placehold.it/15/1fcf00/000000?text=+) **Green** the user can sign in and use company services. 

> ### 3.1 Requests - create/submit
> ![#d66dd3](https://placehold.it/15/d66dd3/000000?text=+) CR/PR and any other users with a position in the contract can create requests to add personnel to the contract.
> If a non-CR/PR user creates the request, the CR/PR has to approve for the request to be submitted to the ![#6f30a0](https://placehold.it/15/6f30a0/000000?text=+) CR for approval.
>  
>  **If a person is not available for requests, the user might not have a green status**

> ### 3.2 Requests - approve
>  ![#6f30a0](https://placehold.it/15/6f30a0/000000?text=+) CR receives the request for approval. The  CR can review the request information, like position information and suggested person. 
>   
>   The ![#6f30a0](https://placehold.it/15/6f30a0/000000?text=+) CR can discuss the request with the ![#d66dd3](https://placehold.it/15/d66dd3/000000?text=+) contractor side and suggest changes. Requests can be edited by the ![#d66dd3](https://placehold.it/15/d66dd3/000000?text=+) contractor side until rejected or approved.

> ### 3.3 Requests - provisioning
> When the ![#6f30a0](https://placehold.it/15/6f30a0/000000?text=+) approves the request, the ![#ff0000](https://placehold.it/15/ff0000/000000?text=+) system will provision the position to the org chart. Once this is done, the user will begin to get access to different systems.
>  
>  **The request will be in the provisioning status until the system confirms this has been done successfully. If nothing happens within a few minutes, a Service Now ticket should be raised**



