/// <reference types="cypress" />

/**
 * External Personnel Landing page
 */
 export default class ExternalPersonnelLandingPage {
    AllocateContractButton(){
        return cy.contains('Allocate contract');
    }

    HelpButton(){
        return cy.get('[data-cy="help-btn"]');
    }
   
}
