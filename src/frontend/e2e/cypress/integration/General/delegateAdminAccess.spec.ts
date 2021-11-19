
// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../support" />

import {delegateAdminAccess, recertifyAdminAccess, removeAdminAccess} from "../../support/delegateAccess"

import NavigationDrawer from "../../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

const person = 'Qi Jin'

describe('TC 13035 Delegate Admin Access', () => {
  /** TODO make login persistent between tests */
  before(() => {
    cy.clearLocalStorage();
    cy.login();
    cy.visit('/');
  });

  beforeEach(() => {
    const contractNo = '312312341'

    cy.loadProject('Query test project')  
    cy.openContract(contractNo)
  })

  it('Delegate Admin Access - Equinor Responsible', () => {
    /** verify the equinor responsible session is visible */
    cy.contains('Contract details').should('be.visible')
    cy.contains('Equinor responsible').should('be.visible')

    /** delegate equinor admin access to a new person */    
    delegateAdminAccess('equinor', person, '1-month')

    /** re-certify to a person existing in the equinor admin access table*/
    recertifyAdminAccess('equinor', person, '6-months')

    /** remove a existing person from the equinor admin access table */
    removeAdminAccess('equinor', person)

    navigationDrawer.CloseContractButton().click()
  });

  it('Delegate Admin Access - External Responsible', () => {
    /** verify the external responsible sessions is visible */
    cy.contains('Contract details').should('be.visible')
    cy.contains('External responsible').should('be.exist')

    /** delegate external admin access to a new person */    
    delegateAdminAccess('external', person, '1-month')

    /** re-certify to a person existing in the external admin access table*/
    recertifyAdminAccess('external', person, '6-months')

    /** remove a existing person from the external admin access table */
    removeAdminAccess('external', person)

    navigationDrawer.CloseContractButton().click()
  });

})