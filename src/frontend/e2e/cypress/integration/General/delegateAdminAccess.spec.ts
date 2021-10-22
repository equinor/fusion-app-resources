
// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../support" />
import ContractDetailGeneralPage from "../../POM/ContractDetailGeneralPage"
const contractDetail = new ContractDetailGeneralPage()

import DelegateAccessSideSheet from "../../POM/DelegateAccessSideSheet"
const delegateSidesheet = new DelegateAccessSideSheet()

import RecertifyPopup from "../../POM/RecertifyPopup"
const recertifyPopup = new RecertifyPopup ()

describe('TC 13035 Delegate Admin Access', () => {
  /** TODO make login persistent between tests */
  before(() => {
    cy.clearLocalStorage();
    cy.login();
    cy.visit('/');
  });

  it('Delegate Admin Access - Equinor Responsible', () => {
    const contractNo = '312312341'
    const DelegatePeople = 'Qi Jin'

    cy.loadProject('Query test project')
    
    cy.openContract(contractNo)

    cy.contains('Contract details').should('be.visible')

    cy.contains('Equinor responsible').should('be.visible')
    contractDetail.EquinorRespDelegateAccess().find('[data-cy="delegate-table"]').should('be.visible')
    cy.wait(1000)
    contractDetail.EquinorRespDelegateButton().click()

    /**  delegate sidesheet should show up */
    delegateSidesheet.DelegateSideSheet().should('be.visible')
    cy.contains('Delegate access').should('be.visible')

    // delegateSidesheet.DelegateButton().invoke('').should('contain', 'disabled')

    delegateSidesheet.Certify6MonthsRadioButton().click()
 
    delegateSidesheet.AddPeopleSelector().type(DelegatePeople)
    cy.get('[class^="fc--SearchableDropdown"]').contains(DelegatePeople).click()
    cy.get('[data-cy="selected-person"]').should('contain', DelegatePeople)

    // delegateSidesheet.DelegateButton().invoke('attr', 'class').should('not.contain', 'disabled')
    delegateSidesheet.DelegateButton().click({force: true})
    cy.wait(1000)

    /** verify the updates in the delegate table */ 
    contractDetail.EquinorRespDelegateAccess().find('[data-cy="assigned-person"]').should('contain', DelegatePeople)
    contractDetail.EquinorRespDelegateAccess().find('[data-cy="recertification-date"]').last().should('contain', '-')

    /** re-certify a person */
    contractDetail.EquinorRespDelegateAccess().find('input[type="checkbox"]').last().click()

    contractDetail.EquinorRespRecertifyButton().click()

    recertifyPopup.RecertifyPopupDropdown().should('be.visible')

    recertifyPopup.Certify1YearRadioButton().click()

    recertifyPopup.ReCertifyButton().click()
    cy.wait(1000)

    contractDetail.EquinorRespDelegateAccess().find('[data-cy="recertification-date"]').last().should('not.contain', '-')

  });

})