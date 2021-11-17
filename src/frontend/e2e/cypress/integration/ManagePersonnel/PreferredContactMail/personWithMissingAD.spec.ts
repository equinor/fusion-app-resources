// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../../support" />

import NavigationDrawer from "../../../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

import PreferredContactMailPage from "../../../POM/PreferredContactMailPage"
const preferredContactMailPage = new PreferredContactMailPage()


describe('TC 23844 - Show/Hide personnel with missing AD', () => {
    /** TODO make login persistent between tests */
    before(() => {
        cy.clearLocalStorage();
        cy.login();
        cy.visit('/');

        const contractNo = '312312341'
        cy.loadProject('Query test project')
        cy.openContract(contractNo)
    });

    it('Show/Hide personnel with no mail', () => {
        /** open the 'preferred contact mail' tab */
        navigationDrawer.PreferredContactMailTab().click().invoke('attr', 'class').should('contain', 'isActive')

        preferredContactMailPage.PersonnelWithMissingAdButton().invoke('attr', 'class').should('contain', 'outlined')

        /** verify that all the person in the table have an approved AD status*/
        preferredContactMailPage.PersonnelMailsTable().within(() => {
            cy.get('[data-cy="ad-column"]').each(($el) => {
                cy.wrap($el).invoke('attr','id').should('eq', 'approved')
            })
        })

        preferredContactMailPage.PersonnelWithMissingAdButton().click().invoke('attr', 'class').should('contain', 'contained')

        /** verify that some person in the table have no AD access */
        preferredContactMailPage.PersonnelMailsTable().within(() => {
            cy.get('[id="no-access"]').should('exist')
        })

        navigationDrawer.CloseContractButton().click()
    });

})