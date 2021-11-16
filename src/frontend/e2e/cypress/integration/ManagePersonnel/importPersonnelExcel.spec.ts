// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../support" />

import path = require("path")

describe('TC 13061 Import Personnel Excel', () => {
    /** TODO make login persistent between tests */
    before(() => {
        cy.deleteDownloadsFolder();

        cy.clearLocalStorage();
        cy.login();
        cy.visit('/');

        const contractNo = '312312341'
        cy.loadProject('Query test project')
        cy.openContract(contractNo)
    });

    it('Download empty excel template, and import an excel with content', () => {
        const file = 'Personnel-Import-Test.xlsx'
        /** open the 'contract personnel' tab */
        cy.get('#contract-personnel-tab').click().invoke('attr', 'class').should('contain', 'isActive')
        cy.wait(100)

        cy.get('#excel-btn').click()
        cy.get('#excel-sidesheet').should('be.visible')

        /** download the excel sample file */
        cy.get('#download-excel-template-btn').click()
        cy.log('**read downloaded file**')
        cy.validateExcelFile('Personnel import.xlsx');

        /** import a valid excel file */
        cy.get('input[type="file"]').attachFile(file)
        cy.get('[data-cy="selected-file-name"]').should('contain', file)
        cy.get('#process-excel-btn').invoke('attr', 'class').should('not.contain', 'disabled')
        cy.get('#process-excel-btn').click()

        /** jump to the add person sidesheet, click save button */
        cy.get('#add-person-sidesheet').should('be.visible').within(() => {
            cy.get('[id="email-input"]').first().should('not.be.empty')
            cy.get('#save-btn').should('not.have.class', 'disabled').click()
        });

        /** in request progress sidesheet */
        cy.get('#add-personnel-request-progress-sidesheet').should('be.visible').within(() => {
            cy.contains('Successful', { timeout: 20 * 1000 }).should('be.visible')
            cy.get('#close-btn').click()
            cy.wait(100)
        });

        /** read the content of the excel file  */
        cy.readExcelFile(file).then(email => {
            console.log('The email is: ', email)

            /** verify the added person from excel exist in the contract personnel table */
            cy.get('[id="email-column"]').should('contain', email.trim())

            /** clean up: close the excel sidesheet and the contract detail page */
            cy.deletePerson(email)
        });

        cy.get('#close-contract-btn').click({ force: true })
    });

})