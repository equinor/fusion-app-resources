/// <reference types="cypress" />

/**
 * Actual MPP Page
 */
 export default class ActualMppPage {
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

    ActualMppTable() {
        return cy.get('#actual-mpp-request-table', {timeout: 15*1000});
    }
    
}