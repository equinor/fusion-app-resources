
// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../support" />
import ContractDetailGeneralPage from "../../POM/ContractDetailGeneralPage"
const contractDetail = new ContractDetailGeneralPage()

describe('TC 13035 Delegate Admin Access', () => {
  /** TODO make login persistent between tests */
  before(() => {
    cy.clearLocalStorage();
    cy.login();
    cy.visit('/');
  });

  it('Delegate Admin Access - Equinor Responsible', () => {
    const projectId = '29ddab36-e7a9-418b-a9e4-8cfbc9591274'
    const contractNo = '312312341'

    cy.loadProject('Query test project')
    
    cy.openContract(contractNo)

    // verify General tab is active
    // give a data-cy to the general tab
    //cy.get('#general-tab').invoke('attr', 'class').should('contain', 'isActive')

    cy.contains('Contract details').should('be.visible')

    cy.contains('Equinor responsible').should('be.visible')
    contractDetail.EquinorRespDelegateAccess().find('[data-cy="delegate-table"]').should('be.visible')
    contractDetail.EquinorRespDelegateButton().click()

    // delegate sidesheet should show up
    
  });

})