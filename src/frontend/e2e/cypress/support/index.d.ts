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
    getDelegateIndex(id: string, text: string): Chainable<number>

    /** search and filters */
    searchText(column: string, keyword: string): Chainable<void>
    disciplineFilter(item: string): Chainable<void>
    adStatusFilter(item: string): Chainable<void>

    /** contract person */
    checkContractPersonExistence(email: string): Chainable<number>
    addContractPerson(firstName: string, lastName: string, email: string, phoneNumber: string): Chainable<void>
    deleteContractPerson(email: string): Chainable<void>

    /** download excel file */
    readExcelFile(filename: string): Chainable<string>

  }
}