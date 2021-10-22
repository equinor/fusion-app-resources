/// <reference types="cypress" />

/**
 * Delegate Access Sidesheet
 */
 export default class DelegateAccessSideSheet {
    DelegateSideSheet(){
        return cy.get('[class^="fc--Modal__modalSideSheet"]');
    }
    
    DelegateButton(){
        return cy.get('[class^="fc--Modal__headerIcons"]').find('button');
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
   
}