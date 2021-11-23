/// <reference types="cypress" />

/**
 * Active Request Page
 */
 export default class ActiveRequestPage {
    AddActiveRequestButton() {
        return cy.get('#add-active-request-btn');
    }

    AddActiveRequestForSelectedPositionButton() {
        return cy.get('#add-active-request-for-selected-position-btn');
    }

    EditActiveRequestButton() {
        return cy.get('#edit-active-request-btn');
    }

    RemoveActiveRequestButton() {
        return cy.get('#remove-active-request-btn');
    }

    RejectActiveRequestButton() {
        return cy.get('#reject-active-request-btn');
    }

    ApproveActiveRequestButton() {
        return cy.get('#approve-active-request-btn');
    }

    HelpButton() {
        return cy.get('[data-cy="help-btn"]');
    }

    ActiveRequestTable() {
        return cy.get('#active-request-table');
    }
    
}