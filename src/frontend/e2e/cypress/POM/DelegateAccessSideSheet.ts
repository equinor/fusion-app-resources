/// <reference types="cypress" />

/**
 * Landing page of a project
 */
 export default class DelegateAccessSideSheet {
    DelegateSideSheet(){
        return cy.contains('Allocate contract');
    }
    
    DelegateButton(){
        return cy.contains('Allocate contract');
    }

    Certify1MonthRadioButton(){
        return cy.get('[data-cy="help-btn"]');
    }

    Certify6MonthsRadioButton(){
        return cy.get('[data-cy="help-btn"]');
    }

    Certify1YearRadioButton(){
        return cy.get('[data-cy="help-btn"]');
    }

    AddPeopleSelector(){
        return cy.get('[data-cy="help-btn"]');
    }
   
}