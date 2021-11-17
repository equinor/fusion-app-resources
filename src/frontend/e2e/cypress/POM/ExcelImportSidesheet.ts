/// <reference types="cypress" />

/**
 * Excel Import Sidesheet
 */
 export default class ExcelImportSidesheet {
    ExcelSidesheet() {
        return cy.get('#excel-sidesheet');
    }
    
    CloseButton() {
        return cy.get('[id="close-btn"]');
    }

    DownloadExcelTemplateButton() {
        return cy.get('#download-excel-template-btn')
    }

    UploadExcelInputArea() {
        return cy.get('input[type="file"]')
    }

    SelectedFileName() {
        return cy.get('[data-cy="selected-file-name"]')
    }

    ProcessExcelButton() {
        return cy.get('#process-excel-btn')
    }
    
}