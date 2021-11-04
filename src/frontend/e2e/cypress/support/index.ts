// ***********************************************************
// This example support/index.js is processed and
// loaded automatically before your test files.
//
// This is a great place to put global configuration and
// behavior that modifies Cypress.
//
// You can change the location of this file or turn off
// automatically serving support files with the
// 'supportFile' configuration option.
//
// You can read more here:
// https://on.cypress.io/configuration
// ***********************************************************

// Import commands.js using ES2015 syntax:
import './commands'
import './delegateAccess'
import './searchAndFilters'
import './managePerson'
import './importFile'

const contractTableColumnSelector = {
    contractId: '[data-cy="contract-id"]',
    companyRep: '[data-cy="company-rep"]',
    contractRep: '[data-cy="contract-rep"]'
}

/**
 * lib com
 */
class TableSelectors<C> {
    constructor(public columns: C){}

    getCell( column: keyof C, row?: number) {
        return [row && `[data-row-index=${row}]`, this.columns[column]].filter(Boolean).join(' ');
    }
}


export const componentSelector = {
    contractTable: new TableSelectors(contractTableColumnSelector)
}