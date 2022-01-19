/// <reference types="cypress" />

/**
 * Completed Request Page
 */
 export default class CompletedRequestPage {
    
    CompletedRequestTable() {
        return cy.get('#completed-request-table', {timeout: 15*1000});
    }
    
}