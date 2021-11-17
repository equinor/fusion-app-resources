/// <reference types="cypress" />

/**
 * Contract Detail General page
 */
 export default class ContractDetailGeneralPage {
    EditContractButton(){
        return cy.get('[data-cy="edit-contract-btn"]');
    }

    HelpButton(){
        return cy.get('[data-cy="help-btn"]');
    }

    EquinorResponsible(){
        return cy.get('[data-cy="equinor-responsible"]');
    }

    EquinorCompanyRep(){
        return cy.get('#equinor-company-rep');
    }

    EquinorContractResp(){
        return cy.get('#equinor-contract-resp');
    }

    EquinorRespDelegateAccess(){
        return cy.get('[data-cy="equinor-resp-delegate-admin-access"]');
    }

    EquinorRespDelegateTable(){
        return cy.get('[data-cy="equinor-resp-delegate-admin-access"]').find('[data-cy="delegate-table"]');
    }

    ExternalResponsible(){
        return cy.get('[data-cy="external-responsible"]');
    }

    ExternalCompanyRep(){
        return cy.get('#external-company-rep');
    }

    ExternalContractResp(){
        return cy.get('#external-contract-resp');
    }

    ExternalRespDelegateAccess(){
        return cy.get('[data-cy="external-resp-delegate-admin-access"]');
    }

    ExternalRespDelegateTable(){
        return cy.get('[data-cy="external-resp-delegate-admin-access"]').find('[data-cy="delegate-table"]');
    }
    
    DelegateTable(){
        return cy.get('[data-cy="delegate-table"]');
    }

    DelegateButton(){
        return cy.get('#delegate-btn');
    }

    RecertifyButton(){
        return cy.get('#recertify-btn');
    }

    RemoveAccessButton(){
        return cy.get('#remove-access-btn');
    }
   
}