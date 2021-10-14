
// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../support" />
import {componentSelector} from "../../support/index"
import ContractDetailGeneralPage from "../../POM/ContractDetailGeneralPage"
const contractDetail = new ContractDetailGeneralPage()

describe('Contract Detail', () => {
  /** TODO make login persistent between tests */
  before(() => {
    cy.clearLocalStorage();
    cy.login();
    cy.visit('/');
  });

  it('TC 13034 Contract Detail', () => {
    const projectId = '29ddab36-e7a9-418b-a9e4-8cfbc9591274'

    cy.loadProject('Query test project')
    // wait for the response
    cy.intercept('GET', '/projects/'+projectId+'/contracts').as('load-project-contracts')
    cy.wait('@load-project-contracts', {timeout:10000})
    
    cy.get(componentSelector.contractTable.getCell('contractId')).should('be.visible').as('contract-id')

    cy.get('@contract-id').first().invoke('attr', 'href').then(($href) => {
        const contractUrl = $href.toString().trim()
        cy.get('@contract-id').first().click()
        cy.intercept('GET', contractUrl).as('load-contract')
        cy.wait('@load-contract')
    })

    // verify General tab is active
    // give a data-cy to the general tab
    //cy.get('#general-tab').invoke('attr', 'class').should('contain', 'isActive')

    cy.contains('Contract details').should('be.visible')

    cy.contains('Equinor responsible').should('be.visible').as('equinor-resp')
    cy.get('@equinor-resp').next().find('[class^="fc--PositionCard__context"]').its('length').should('be.greaterThan', 0)
    contractDetail.EquinorRespDelegate().find('[data-cy="delegate-table"]').should('be.visible')


    cy.contains('External responsible').should('be.exist').as('external-resp')
    // TODO: give a data-cy to the position card
    cy.get('@external-resp').next().find('[class^="fc--PositionCard__context"]').its('length').should('be.greaterThan', 0)
    contractDetail.ExternalRespDelegate().find('[data-cy="delegate-table"]').should('be.exist')
    
  });

})