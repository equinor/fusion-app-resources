
// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../support" />

describe('TC 13029 - Choose Project', () => {
  /** TODO make login persistent between tests */
  before(() => {
    cy.clearLocalStorage();
    cy.login();
    cy.visit('/');
  });

  it('Choose a project', () => {
    /**  select a project and load data */
    const projectName = 'Query test project'

    cy.loadProject(projectName)
    
    cy.get('[data-cy="contract-id"]').should('be.visible')
    
  });

})