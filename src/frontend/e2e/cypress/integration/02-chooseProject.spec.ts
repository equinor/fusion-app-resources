
// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../support" />
import {componentSelector} from "../support/index"

describe('Choose Project', () => {
  /** TODO make login persistent between tests */
  before(() => {
    cy.clearLocalStorage();
    cy.login();
    cy.visit('/');
  });

  it('TC 13029 Choose project', () => {
    // select a project and load data
    const projectName = 'Query test project'
    const projectId = '29ddab36-e7a9-418b-a9e4-8cfbc9591274'

    cy.loadProject(projectName)
    // wait for the response
    cy.intercept('GET', '/projects/'+projectId+'/contracts').as('load-project-contracts')
    cy.wait('@load-project-contracts')
    
    cy.get(componentSelector.contractTable.getCell('contractId')).should('be.visible')
    
  });

})