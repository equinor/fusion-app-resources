
// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../support" />
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
    const contractNo = '312312341'

    cy.loadProject('Query test project')
    
    cy.openContract(contractNo)

    /** verify General tab is active */
    //cy.get('#general-tab').invoke('attr', 'class').should('contain', 'isActive')

    cy.contains('Contract details').should('be.visible')

    cy.contains('Equinor responsible').should('be.visible')
    contractDetail.EquinorResponsible().find('[class^="fc--PositionCard__context"]').its('length').should('be.greaterThan', 0)
    contractDetail.EquinorRespDelegateAccess().find('[data-cy="delegate-table"]').should('be.visible')


    cy.contains('External responsible').should('be.exist').as('external-resp')
    contractDetail.ExternalResponsible().find('[class^="fc--PositionCard__context"]').its('length').should('be.greaterThan', 0)
    contractDetail.ExternalRespDelegateAccess().find('[data-cy="delegate-table"]').should('be.exist')
    
  });

})