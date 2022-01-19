/// <reference types="cypress" />

/**
 * Reject Request Sidesheet
 */
 export default class RejectRequestSidesheet {
    RejectRequestSidesheet() {
        return cy.get('#reject-personnel-sidesheet');
    }

    RejectReasonTextArea() {
        return cy.get('textarea').first()
    }

    ConfrimRejectionButton() {
        return cy.get('#confirm-rejection-btn')
    }

    CloseSidesheetButton() {
        return cy.get('[id="close-btn"]');
    }    
}