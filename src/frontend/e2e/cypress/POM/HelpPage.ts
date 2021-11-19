/// <reference types="cypress" />

/**
 * Help Page
 */
 export default class HelpPage {
    ContractManagementTab() {
        return cy.get('#contract-management-tab', { timeout: 10000 });
    }

    RoleDelegationTab() {
        return cy.get('#role-delegation-tab');
    }
    
}