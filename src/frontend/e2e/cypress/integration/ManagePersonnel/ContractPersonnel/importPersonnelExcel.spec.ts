// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../../support" />

import path = require("path")

import NavigationDrawer from "../../../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

import ContractPersonnelPage from "../../../POM/ContractPersonnelPage"
const contractPersonnelPage = new ContractPersonnelPage()

import AddEditPersonSidesheet from "../../../POM/AddPersonSidesheet"
const AddPersonSidesheet = new AddEditPersonSidesheet()

import ExcelImportSidesheet from "../../../POM/ExcelImportSidesheet"
const excelImportSidesheet = new ExcelImportSidesheet()

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
        const downloadedFile = 'Personnel import.xlsx'
        const uploadFile = 'Personnel-Import-Test.xlsx'
        /** open the 'contract personnel' tab */
        navigationDrawer.ContractPersonnelTab().click().invoke('attr', 'class').should('contain', 'isActive')
        cy.wait(100)

        contractPersonnelPage.ExcelButton().click()
        excelImportSidesheet.ExcelSidesheet().should('be.visible')

        /** download the excel sample file */
        excelImportSidesheet.DownloadExcelTemplateButton().click()
        cy.log('**read downloaded file**')
        cy.validateExcelFile(downloadedFile);

        /** import a valid excel file */
        excelImportSidesheet.UploadExcelInputArea().attachFile(uploadFile)
        excelImportSidesheet.SelectedFileName().should('contain', uploadFile)
        excelImportSidesheet.ProcessExcelButton().invoke('attr', 'class').should('not.contain', 'disabled')
        excelImportSidesheet.ProcessExcelButton().click()

        /** jump to the add person sidesheet, click save button */
        AddPersonSidesheet.AddPersonSidesheet().should('be.visible').within(() => {
            AddPersonSidesheet.EmailInputBox().first().should('not.be.empty')
            AddPersonSidesheet.SaveButton().should('not.have.class', 'disabled').click()
        });

        /** in request progress sidesheet */
        AddPersonSidesheet.RequestProgressSidesheet().should('be.visible').within(() => {
            cy.contains('Successful', { timeout: 20 * 1000 }).should('be.visible')
            AddPersonSidesheet.CloseButton().click()
            cy.wait(100)
        });

        /** read the content of the excel file  */
        cy.readExcelFile(uploadFile).then(email => {
            console.log('The email is: ', email)

            /** verify the added person from excel exist in the contract personnel table */
            cy.get('[id="email-column"]').should('contain', email.trim())

            /** clean up: close the excel sidesheet and the contract detail page */
            cy.deletePerson(email)
        });

        navigationDrawer.CloseContractButton().click({ force: true })
    });

})