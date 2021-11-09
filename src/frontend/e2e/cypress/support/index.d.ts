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
     login(): Chainable<void>
     fusion(): Chainable<Fusion>
     currentUser(): Chainable<{id: string, fullName: string}>
     loadProject(name: string): Chainable<void> 
     openContract(number: string): Chainable<void>
     delegateAdminAccess(responsible: string, person: string, period: string): Chainable<void>
     recertifyAdminAccess(responsible: string, person: string, period: string): Chainable<void>
     removeAdminAccess(responsible: string, person: string): Chainable<void>
     getDelegateIndex(id: string, text: string): Chainable<number>
     // getPersonIndex(column: string, keyword: string): Chainable<number>
     searchText(column: string, keyword: string): Chainable<void>
     disciplineFilter(item: string): Chainable<void>
     adStatusFilter(item: string): Chainable<void>
     fillPersonData(index:number, type: string, data: string): Chainable<void>
  }
}