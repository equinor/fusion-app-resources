/// <reference types="cypress" />

/**
 * Actual MPP Page
 */
 export default class ActualMppPage {
    AddMppRequestButton() {
        return cy.get('#add-mpp-request-btn');
    }

    AddMppRequestForSelectedPositionButton() {
        return cy.get('#add-mpp-request-for-selected-position-btn');
    }

    EditMppRequestButton() {
        return cy.get('#edit-mpp-request-btn');
    }

    RemoveMppRequestButton() {
        return cy.get('#remove-mpp-request-btn');
    }

    ActualMppTable() {
        return cy.get('#actual-mpp-request-table');
    }
    
}