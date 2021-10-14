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

    EquinorRespDelegate(){
        return cy.get('[data-cy="equinor-resp-delegate-admin-access"]');
    }

    ExternalRespDelegate(){
        return cy.get('[data-cy="external-resp-delegate-admin-access"]');
    }
   
}