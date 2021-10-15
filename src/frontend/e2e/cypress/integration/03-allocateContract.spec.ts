// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../support" />
import {componentSelector} from "../support/index"
import ExternalPersonnelLandingPage from "../POM/ExternalPersonnelLandingPage"
import ContractEditPage from "../POM/ContractEditPage"
import { contains } from "cypress/types/jquery"

const externalPersonnelPage = new ExternalPersonnelLandingPage()
const allocateContractPage = new ContractEditPage()

describe('Allocate contract', () => {
  /** TODO make login persistent between tests */
  before(() => {
    cy.clearLocalStorage();
    cy.login();
    cy.visit('/');
  });

  it('TC 13030 Allocate Project', () => {
    // select a project and load data
    const projectName = 'Query test project'
    const projectId = '29ddab36-e7a9-418b-a9e4-8cfbc9591274'
    const contractNo = 'xxxxx' // fill in the contract number later

    cy.loadProject(projectName)
    // wait for the response
    cy.intercept('GET', '/projects/'+projectId+'/contracts').as('load-project-contracts')
    cy.wait('@load-project-contracts')
    
    externalPersonnelPage.AllocateContractButton().click()
    cy.contains('h2', 'Allocate a Contract').should('be.visible')
    allocateContractPage.Step1Button().invoke('attr', 'class').should('contain', 'current')

    allocateContractPage.ContractSelector().type(contractNo)

    // fill in the api later
    cy.intercept('GET', 'api').as('load-contract-dropdown')
    cy.wait('@load-contract-dropdown')

    // get the contract drop down menu element
    cy.get('ContractDropdownMenu').contains(contractNo).click()

    // expect the contract no is selected, and move to step 2
    allocateContractPage.Step2Button().invoke('attr', 'class').should('contain', 'current')
    
  });

})