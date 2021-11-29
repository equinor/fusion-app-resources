// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../../support" />

import NavigationDrawer from "../../../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

describe('Active Requests - Search and Filter', () => {
    /** TODO make login persistent between tests */
    before(() => {
        cy.clearLocalStorage();
        cy.login();
        cy.visit('/');
    });

    it('TC 24128 Search and Filter', () => {
        const contractNo = '312312341'

        cy.loadProject('Query test project')
        cy.openContract(contractNo)

        /** open the 'Active Requests' tab */
        navigationDrawer.ActiveRequestsTab().click().invoke('attr', 'class').should('contain', 'isActive')
        navigationDrawer.CollapseExpandButton().click()
        cy.wait(100)
        
        /** search keyword */
        cy.searchText('base-position', 'Automation Designer')

        /** disciplines filters */
        cy.disciplineFilter('Aut/Inst/Tele')
        cy.disciplineFilter('Engineering Management')

        /** request status filters */
        cy.requestStatusFilter('Created')
        cy.requestStatusFilter('SubmittedToCompany')

    });
})