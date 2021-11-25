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

    RequestGeneralTabContent() {
        return cy.get('[data-cy="request-general-tab-content"]')
    }

    RequestDescriptionTab() {
        return cy.get('#request-description-tab')
    }

    RequestDescriptionTabContent() {
        return cy.get('[data-cy="request-description-tab-content"]')
    }

    RequestPersonTab() {
        return cy.get('#request-person-tab')
    }
    
    RequestPersonTabContent() {
        return cy.get('[data-cy="request-person-tab-content"]')
    }

    CloseSidesheetButton() {
        return cy.get('[id="close-btn"]');
    }    
}