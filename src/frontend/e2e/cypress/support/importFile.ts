/**  type definitions for Cypress object "cy" */
/// <reference types="cypress" />

/** @ts-check */
import path = require('path')

/**
 * Delete the downloads folder to make sure the test has "clean"
 * slate before starting.
 */
Cypress.Commands.add('deleteDownloadsFolder', () => {
    const downloadsFolder = Cypress.config('downloadsFolder')
    cy.task('deleteFolder', downloadsFolder)
});

/**
 * Validate the downloaded excel has the valid content
 */
Cypress.Commands.add('validateExcelFile', (filename) => {
    const downloadsFolder = Cypress.config('downloadsFolder')
    const downloadedFilename = path.join(downloadsFolder, filename.trim())

    /** ensure the file has been saved before trying to parse it */
    cy.readFile(downloadedFilename, 'binary', { timeout: 15000 })
        .should((buffer) => {
            expect(buffer.length).to.be.gt(1000)
        })

    cy.log('**the file exists**')

    cy.task('readExcelFile', downloadedFilename)
        /** returns an array of lines read from Excel file */
        .should('have.length', 1)
        .then((list) => {
            expect(list[0], 'header line').to.deep.equal([
                'Email', 'Firstname', 'Lastname', 'Disciplines', 'Dawinci', 'Phone number',
            ])
        })
});

Cypress.Commands.add('readExcelFile', (filename) => {
    cy.task('readExcelFile', 'cypress/fixtures/'+filename).then((lists) => {
        /** returns an array of lines read from Excel file */
        console.log('the number of rows in excel: ', lists.length)
        expect(lists[1][0]).to.equal('person3@excel.com')
        
        const email = lists[1][0]
        console.log(email)

        cy.wrap(email)
    })
});

