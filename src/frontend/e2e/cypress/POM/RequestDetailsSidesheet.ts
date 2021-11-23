/// <reference types="cypress" />

/**
 * Request Details Sidesheet
 */
 export default class RequestDetailsSidesheet {
    RequestDetailsSidesheet() {
        return cy.get('#request-details-sidesheet');
    }

    RejectRequestButton() {
        return cy.get('#reject-request-btn')
    }

    ApproveRequestButton() {
        return cy.get('#approve-request-btn')
    }
    
    RequestGeneralTab() {
        return cy.get('#request-general-tab')
    }

    RequestDescriptionTab() {
        return cy.get('#request-description-tab')
    }

    RequestPersonTab() {
        return cy.get('#request-person-tab')
    }
    
    CloseSidesheetButton() {
        return cy.get('[id="close-btn"]');
    }
    
}