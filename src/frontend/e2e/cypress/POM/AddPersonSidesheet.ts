/// <reference types="cypress" />

/**
 * Add/Edit person sidesheet
 */
 export default class AddEditPersonSidesheet {
    AddPersonSidesheet() {
        return cy.get('#add-person-sidesheet');
    }

    AddPersonButton() {
        return cy.get('#add-person-btn');
    }

    SaveButton() {
        return cy.get('#save-btn');
    }

    CloseSidesheetButton() {
        return cy.get('#close-btn');
    }

    AddPersonTable() {
        return cy.get('[data-cy="add-person-table"]');
    }

    EmailInputBox() {
        return cy.get('[id="email-input"]');
    }

    RequestProgressSidesheet() {
        return cy.get('#add-personnel-request-progress-sidesheet')
    }

    
}