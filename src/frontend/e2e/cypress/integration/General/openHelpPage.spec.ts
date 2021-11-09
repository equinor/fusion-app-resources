
// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../support" />
import ExternalPersonnelLandingPage from "../../POM/ExternalPersonnelLandingPage"
const externalPersonnelPage = new ExternalPersonnelLandingPage()

describe('Help Page', () => {
  /** TODO make login persistent between tests */
  before(() => {
    cy.clearLocalStorage();
    cy.login();
    cy.visit('/');
  });

  it('TC 13042 Open the Help page', () => {

    cy.loadProject('Query test project')

    externalPersonnelPage.HelpButton().invoke('removeAttr', 'target').click()

    cy.url().should('include', '/help')

    cy.get('#contract-management-tab', { timeout: 5000 }).invoke('attr', 'class').should('contain', 'Tabs__current')
    cy.contains('h1', 'Contract Management').should('be.visible')

    cy.get('#role-delegation-tab').click()
      .invoke('attr', 'class').should('contain', 'Tabs__current')
    cy.contains('h1', 'Role Delegation').should('be.visible')

  });

})