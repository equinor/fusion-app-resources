/// <reference types="cypress" />

/**
 * Contract Detail General page
 */
 export default class ContractDetailGeneralPage {
    GeneralTab(){
        return cy.get('#general-tab');
    }

    EditButton(){
        return cy.get('[data-cy="edit-btn"]');
    }

    HelpButton(){
        return cy.get('[data-cy="help-btn"]');
    }

    CloseButton(){
        return cy.get('[data-cy="close-btn"]');
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

    EquinorRespDelegateButton(){
        return cy.get('[data-cy="equinor-resp-delegate-admin-access"]').find('button').contains('Delegate');
    }

    EquinorRespRecertifyButton(){
        return cy.get('[data-cy="equinor-resp-delegate-admin-access"]').find('button').contains('Re-certify');
    }

    EquinorRespRemoveButton(){
        return cy.get('[data-cy="equinor-resp-delegate-admin-access"]').find('button').contains('Remove');
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

    ExternalRespDelegateButton(){
        return cy.get('[data-cy="external-resp-delegate-admin-access"]').find('button').contains('Delegate');
    }

    ExternalRespRecertifyButton(){
        return cy.get('[data-cy="external-resp-delegate-admin-access"]').find('button').contains('Re-certify');
    }

    ExternalRespRemoveButton(){
        return cy.get('[data-cy="external-resp-delegate-admin-access"]').find('button').contains('Remove');
    }
   
}