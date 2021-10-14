
// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../support" />
import {componentSelector} from "../../support/index"
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
    const projectId = '29ddab36-e7a9-418b-a9e4-8cfbc9591274'
    const tab1 = 'Contract management'
    const tab2 = 'Role delegation'

    cy.loadProject('Query test project')
    // wait for the response
    cy.intercept('GET', '/projects/'+projectId+'/contracts').as('load-project-contracts')
    cy.wait('@load-project-contracts', {timeout:10000})
    
    externalPersonnelPage.HelpButton().invoke('removeAttr', 'target').click()
    cy.url().should('include', '/help')

    cy.contains('div', tab1).parent('a')
    .invoke('attr', 'class').should('contain', 'Tabs__current')
    cy.contains('h1', 'Contract Management').should('be.visible')

    cy.contains('div', tab2).click()
    cy.contains('div', tab2).parent('a').invoke('attr', 'class').should('contain', 'Tabs__current')
    cy.contains('h1', 'Role Delegation').should('be.visible')
    
  });

})