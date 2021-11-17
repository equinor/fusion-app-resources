/// <reference types="cypress" />

/**
 * Preferred Contact Mail Page
 */
 export default class PreferredContactMailPage {
    PersonnelWithNoMailButton() {
        return cy.get('#personnel-no-mail-btn');
    }

    PersonnelWithMissingAdButton() {
        return cy.get('#show-missing-ad-btn');
    }

    SaveButton() {
        return cy.get('#save-contact-mail-btn');
    }

    PersonnelMailsTable() {
        return cy.get('#personnel-mails-table');
    }
}