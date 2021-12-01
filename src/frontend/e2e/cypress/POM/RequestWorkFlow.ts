/// <reference types="cypress" />

/**
 * Request workflow
 */
 export default class RequestWorkFlow {
    RequestStep1() {
        return cy.get('#created');
    }

    RequestStep2() {
        return cy.get('#contractorApproval');
    }

    RequestStep3() {
        return cy.get('#companyApproval');
    }

    RequestStep4() {
        return cy.get('#provisioning');
    }
}