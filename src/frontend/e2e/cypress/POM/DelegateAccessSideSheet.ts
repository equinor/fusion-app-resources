/// <reference types="cypress" />

/**
 * Delegate Access Sidesheet
 */
 export default class DelegateAccessSideSheet {
    DelegateSideSheet(){
        return cy.get('#delegate-sidesheet');
    }
    
    DelegateButton(){
        return cy.get('#delegate-btn');
    }

    Certify1MonthRadioButton(){
        return cy.get('[data-cy="certify-1-month"]');
    }

    Certify6MonthsRadioButton(){
        return cy.get('[data-cy="certify-6-months"]');
    }

    Certify1YearRadioButton(){
        return cy.get('[data-cy="certify-12-months"]');
    }

    AddPeopleSelector(){
        return cy.get('#add-people').find('[data-cy="person-picker"]');
    }

    RemovePeopleButton(){
        return cy.get('[data-cy="remove-person-btn"]');
    }

    SelectedPerson(){
        return cy.get('[data-cy="selected-person"]');
    }

    CloseSidesheetButton(){
        return cy.get('#close-btn');
    }
   
}