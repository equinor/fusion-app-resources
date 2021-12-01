/// <reference types="cypress" />

/**
 * Create request sidesheet
 */
 export default class CreateRequestSidesheet {
    CreateRequestSidesheet() {
        return cy.get('#edit-request-sidesheet');
    }

    AddRequestItemButton() {
        return cy.get('#add-request-item-btn');
    }

    CopyRequestItemButton() {
        return cy.get('#copy-request-item-btn');
    }

    RemoveRequestItemButton() {
        return cy.get('#remove-request-item-btn');
    }

    HelpButton() {
        return cy.get('[data-cy="help-btn"]');
    }

    SubmitButton() {
        return cy.get('#submit-btn');
    }

    RequestProgressSidesheet() {
        return cy.get('#mpp-request-progress-sidesheet', {timeout: 15*1000});
    }

    CloseSidesheetButton() {
        return cy.get('#close-btn')
    }

    RequestTable() {
        return cy.get('[data-cy="edit-request-table"]', {timeout: 15*1000})
    }
    
}