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
        return cy.get('[id="close-contract-btn"]');
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
        return cy.get('[data-cy="equinor-resp-delegate-admin-access"]').find('#delegate-btn');
    }

    EquinorRespRecertifyButton(){
        return cy.get('[data-cy="equinor-resp-delegate-admin-access"]').find('#recertify-btn');
    }

    EquinorRespRemoveButton(){
        return cy.get('[data-cy="equinor-resp-delegate-admin-access"]').find('#remove-btn');
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
        return cy.get('[data-cy="external-resp-delegate-admin-access"]').find('#delegate-btn');
    }

    ExternalRespRecertifyButton(){
        return cy.get('[data-cy="external-resp-delegate-admin-access"]').find('#recertify-btn');
    }

    ExternalRespRemoveButton(){
        return cy.get('[data-cy="external-resp-delegate-admin-access"]').find('#remove-btn');
    }
   
}