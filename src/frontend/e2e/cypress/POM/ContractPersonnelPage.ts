/// <reference types="cypress" />

/**
 * Contract Personnel page
 */
export default class ContractPersonnelPage {
    AddContractPersonButton() {
        return cy.get('#add-contract-person-btn');
    }

    ExcelButton() {
        return cy.get('#excel-btn');
    }

    DeleteContractPersonButton() {
        return cy.get('#delete-contract-person-btn');
    }

    EditContractPersonButton() {
        return cy.get('#edit-contract-person-btn');
    }

    ContractPersonnelTable() {
        return cy.get('#contract-personnel-table');
    }
}