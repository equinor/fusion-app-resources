/// <reference types="cypress" />

/**
 * Landing page of a project
 */
 export default class ContractDetailGeneralPage {
    GeneralButton(){
        return cy.get('[class^="fc--components__container"]').first();
    }

    EditButton(){
        return cy.get('[data-cy="help-btn"]');
    }
   
}