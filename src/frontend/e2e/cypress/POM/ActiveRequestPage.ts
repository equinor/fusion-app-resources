/// <reference types="cypress" />

/**
 * Active Request Page
 */
 export default class ActiveRequestPage {
    AddRequestButton() {
        return cy.get('#add-request-btn');
    }

    AddRequestForSelectedPositionButton() {
        return cy.get('#add-request-for-selected-position-btn');
    }

    EditRequestButton() {
        return cy.get('#edit-request-btn');
    }

    RemoveRequestButton() {
        return cy.get('#remove-request-btn');
    }

    RejectRequestButton() {
        return cy.get('#reject-request-btn');
    }

    ApproveRequestButton() {
        return cy.get('#approve-request-btn');
    }

    HelpButton() {
        return cy.get('[data-cy="help-btn"]');
    }

    ActiveRequestTable() {
        return cy.get('#active-request-table', {timeout: 15*1000});
    }
    
}