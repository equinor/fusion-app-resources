// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../../support" />

import NavigationDrawer from "../../../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

describe('Contract Personnel - Search and Filter', () => {
    /** TODO make login persistent between tests */
    before(() => {
        cy.clearLocalStorage();
        cy.login();
        cy.visit('/');
    });

    it('TC 13064 Search and Filter', () => {
        const contractNo = '312312341'

        cy.loadProject('Query test project')
        cy.openContract(contractNo)

        /** open the 'contract personnel' tab */
        navigationDrawer.ContractPersonnelTab().click().invoke('attr', 'class').should('contain', 'isActive')
        cy.wait(100)
        
        /** search keyword */
        cy.searchText('first-name', 'Mia')

        /** disciplines filters */
        cy.disciplineFilter('Electro')
        cy.disciplineFilter('IT')
        cy.disciplineFilter('Aut/Inst/Tele')
        cy.disciplineFilter('Safety')
        cy.disciplineFilter('Engineering Management')
        cy.disciplineFilter('Estimation')

        /** AD status filters */
        cy.adStatusFilter('Azure AD Approved')
        cy.adStatusFilter('No Azure Access')
    });
})