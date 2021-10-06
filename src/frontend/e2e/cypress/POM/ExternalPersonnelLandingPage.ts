/// <reference types="cypress" />

/**
 * Landing page of a project
 */
 export default class ExternalPersonnelLandingPage {
    AllocateContractButton(){
        return cy.contains('Allocate contract');
    }

    HelpButton(){
        return cy.get('[data-cy="help-btn"]');
    }
   
}
