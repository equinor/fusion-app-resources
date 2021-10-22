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
     recertifyAdminAccess(responsible: string, personIndex: number, period: string): Chainable<void>
     removeAdminAccess(responsible: string, personIndex: number): Chainable<void>
  }
}