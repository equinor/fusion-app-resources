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
  }
}