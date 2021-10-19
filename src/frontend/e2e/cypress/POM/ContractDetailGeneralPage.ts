/// <reference types="cypress" />

/**
 * Landing page of a project
 */
 export default class ContractDetailGeneralPage {
    GeneralButton(){
        return cy.contains('General');
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

    EquinorRespDelegate(){
        return cy.get('[data-cy="equinor-resp-delegate-admin-access"]');
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

    ExternalRespDelegate(){
        return cy.get('[data-cy="external-resp-delegate-admin-access"]');
    }
   
}