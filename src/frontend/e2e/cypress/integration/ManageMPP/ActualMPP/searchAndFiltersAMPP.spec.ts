// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../../support" />

import NavigationDrawer from "../../../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

describe('Actual MPP - Search and Filter', () => {
    /** TODO make login persistent between tests */
    before(() => {
        cy.clearLocalStorage();
        cy.login();
        cy.visit('/');
    });

    it('TC 13085 Search and Filter', () => {
        const contractNo = '312312341'

        cy.loadProject('Query test project')
        cy.openContract(contractNo)

        /** open the 'Actual MPP' tab */
        navigationDrawer.ActualMPPTab().click().invoke('attr', 'class').should('contain', 'isActive')
        cy.wait(100)
        
        /** search keyword */
        cy.searchText('position', 'Area Manager')

        /** disciplines filters */
        cy.disciplineFilter('IT')
        cy.disciplineFilter('Aut/Inst/Tele')
        cy.disciplineFilter('Engineering Management')

    });
})