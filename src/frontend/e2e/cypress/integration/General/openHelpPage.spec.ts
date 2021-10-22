
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
    const tab1 = 'Contract management'
    const tab2 = 'Role delegation'

    cy.loadProject('Query test project')
    
    externalPersonnelPage.HelpButton().invoke('removeAttr', 'target').click()

    cy.intercept('GET', '/help').as('load-help')
    cy.wait('@load-help', {timeout:10000})

    cy.url().should('include', '/help')

    cy.contains('div', tab1).parent('a')
    .invoke('attr', 'class').should('contain', 'Tabs__current')
    cy.contains('h1', 'Contract Management').should('be.visible')

    cy.contains('div', tab2).click()
    cy.contains('div', tab2).parent('a').invoke('attr', 'class').should('contain', 'Tabs__current')
    cy.contains('h1', 'Role Delegation').should('be.visible')
    
  });

})