/// <reference types="cypress" />

type Fusion = {
  auth: {
    container: {
      getCachedUserAsync: () => Promise<{}>
    }
  }
}

declare namespace Cypress {
  interface Chainable<Subject> {
    /** general functions */
    login(): Chainable<void>
    fusion(): Chainable<Fusion>
    currentUser(): Chainable<{ id: string, fullName: string }>
    loadProject(name: string): Chainable<void>
    openContract(number: string): Chainable<void>
    collapseExpandSidesheets(): Chainable<void>

    /** delegate admin access */
    delegateAdminAccess(responsible: string, person: string, period: string): Chainable<void>
    recertifyAdminAccess(responsible: string, person: string, period: string): Chainable<void>
    removeAdminAccess(responsible: string, person: string): Chainable<void>
    getDelegateIndex(id: string, text: string): Chainable<number>

    /** search and filters */
    searchText(column: string, keyword: string): Chainable<void>
    disciplineFilter(item: string): Chainable<void>
    adStatusFilter(item: string): Chainable<void>

    /** contract person */
    fillPersonData(index: number, type: string, data: string): Chainable<void>
    deletePerson(email: string): Chainable<void>

    /** download excel file */
    deleteDownloadsFolder(): Chainable<void>
    validateExcelFile(filename: string): Chainable<void>
    readExcelFile(filename: string): Chainable<string>

  }
}