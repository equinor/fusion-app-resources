// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../../support" />


describe('TC 23843 - Personnel with no mail', () => {
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
        cy.get('#preferred-contact-mail-tab').click().invoke('attr', 'class').should('contain', 'isActive')

        cy.get('#personnel-no-mail-btn').invoke('attr', 'class').should('contain', 'outlined')
        cy.get('#personnel-no-mail-btn').click()

        /** verify that all the filtered results in the table have a red close circle icon in the'Equinor mail' column */
        cy.get('#personnel-mails-table').within(() => {
            cy.get('[data-cy="equinor-mail-column"]').each(($el) => {
                cy.wrap($el).invoke('attr', 'data-testid').should('eq', 'close-circle')
            })
        })

        cy.get('#close-contract-btn').click()
    });

})