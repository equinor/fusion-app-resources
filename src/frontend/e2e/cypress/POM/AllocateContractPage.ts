/// <reference types="cypress" />

/**
 * Landing page of a project
 */
 export default class AllocateContractPage {
    CloseButton(){
        return cy.get('[data-cy="close-btn"]');
    }

    SaveButton(){
        return cy.contains('Save');
    }

    CancelButton(){
        return cy.contains('Cancel');
    }

    Step1Button(){
        return cy.get('a[class^="fc--Stepper__step]').first();
    }

    ContractSelector(){
        return cy.get('[class^="fc--TextInput__inputContent]');
    }

    ContractDropdownMenu(){
        return cy.contains('Allocate contract');
    }

    Step2Button(){
        return cy.get('a[class^="fc--Stepper__step]').eq(1);
    }
   
}