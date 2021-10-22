/// <reference types="cypress" />

/**
 * Re-certify popup
 */
 export default class RecertifyPopup {
    RecertifyPopupDropdown(){
        return cy.get('[data-cy="recertify-popup"]');
    }
    
    Certify1MonthRadioButton(){
        return cy.get('[data-cy="recertify-popup"]').find('[data-cy="certify-1-month"]');
    }

    Certify6MonthsRadioButton(){
        return cy.get('[data-cy="recertify-popup"]').find('[data-cy="certify-6-months"]');
    }

    Certify1YearRadioButton(){
        return cy.get('[data-cy="recertify-popup"]').find('[data-cy="certify-12-months"]');
    }

    ReCertifyButton(){
        return cy.get('[data-cy="recertify-popup"]').find('[data-cy="recertify-btn"]');
    }
   
}