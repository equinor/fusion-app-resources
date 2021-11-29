/// <reference types="cypress" />

/**
 * Navigation Drawer
 */
 export default class NavigationDrawer {
    CloseContractButton() {
        return cy.get('[id="close-contract-btn"]');
    }

    CollapseExpandButton(){
        return cy.get('[class^="fc--NavigationDrawer__collapseButtonContainer"]').find('button')
    }

    GeneralTab(){
        return cy.get('#general-tab');
    }

    ContractPersonnelTab() {
        return cy.get('#contract-personnel-tab');
    }

    PreferredContactMailTab() {
        return cy.get('#preferred-contact-mail-tab');
    }

    ActualMPPTab() {
        return cy.get('#actual-mpp-tab');
    }

    ActiveRequestsTab() {
        return cy.get('#active-requests-tab');
    }

    ProvisioningRequestsTab() {
        return cy.get('#provisioning-tab');
    }

    CompletedRequestsTab() {
        return cy.get('#completed-requests-tab');
    }
}