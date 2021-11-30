// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../../support" />

import NavigationDrawer from "../../../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

describe('Completed Requests - Search and Filter', () => {
    /** TODO make login persistent between tests */
    before(() => {
        cy.clearLocalStorage();
        cy.login();
        cy.visit('/');
    });

    it('TC 24130 Search and Filter', () => {
        const contractNo = '312312341'

        cy.loadProject('Query test project')
        cy.openContract(contractNo)

        /** open the 'Actual MPP' tab */
        navigationDrawer.CompletedRequestsTab().click().invoke('attr', 'class').should('contain', 'isActive')
        navigationDrawer.CollapseExpandButton().click()
        cy.wait(100)
        
        /** search keyword */
        cy.searchText('person', 'Odin')

        /** request status filter */
        cy.requestStatusFilter('ApprovedByCompany')
        cy.requestStatusFilter('RejectedByContractor')
        cy.requestStatusFilter('RejectedByCompany')


        /** disciplines filters */
        cy.disciplineFilter('Aut/Inst/Tele')
        cy.disciplineFilter('Engineering Management')

    });
})