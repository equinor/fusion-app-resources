// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../support" />

describe('TC 13060 - Add a new person', () => {
    /** TODO make login persistent between tests */
    before(() => {
        cy.clearLocalStorage();
        cy.login();
        cy.visit('/');
    });

    beforeEach(function () {
        cy.fixture('PersonData.json').then((personData) => {
          this.personData = personData
        })
    });

    it('Add a new person', () => {
        const contractNo = '312312341'

        cy.loadProject('Query test project')
        cy.openContract(contractNo)

        /** open the 'contract personnel' tab */
        cy.get('#contract-personnel-tab').click().invoke('attr', 'class').should('contain', 'isActive')
        //cy.wait(100)

        cy.get('#add-btn').click()

        cy.get('#add-person-sidesheet').should('be.visible').within(() => {
            //cy.wait(100)
            cy.get('#first-name-input').first().type(this.personData.FirstName)
            cy.get('#last-name-input').first().type(this.personData.LastName)
            cy.get('#email-input').first().type(this.personData.Email)
            cy.get('#phone-input').first().type(this.personData.PhoneNumber)

            cy.get('#save-btn').should('not.have.class', 'disabled').click()            
        });

        // in request progress sidesheet
        cy.get('#request-progress-sidesheet').should('be.visible').within(() => {
            cy.wait(3000) // wait for the process to finish
            cy.contains('Successful').should('be.visible')
            cy.get('#close-btn').click()

        });

        cy.get('#contract-personnel-table').find('first-name-column').should('contain', this.personData.FirstName.trim())
    });

})