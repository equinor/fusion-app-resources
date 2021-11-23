
// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../support" />

import {verifyHelpPage} from "../../support/commands"

import ExternalPersonnelLandingPage from "../../POM/ExternalPersonnelLandingPage"
const externalPersonnelPage = new ExternalPersonnelLandingPage()

import NavigationDrawer from "../../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

import ContractDetailGeneralPage from "../../POM/ContractDetailGeneralPage"
const contractDetailGeneralPage = new ContractDetailGeneralPage()

import ActualMppPage from "../../POM/ActualMppPage"
const actualMppPage = new ActualMppPage()

import ActiveRequestPage from "../../POM/ActiveRequestPage"
const activeRequestPage = new ActiveRequestPage()

import CreateRequestSidesheet from "../../POM/CreateRequestSidesheet"
const createRequestSidesheet = new CreateRequestSidesheet()

describe('Verify Help link from different pages', () => {
  /** TODO make login persistent between tests */
  beforeEach(() => {
    cy.clearLocalStorage();
    cy.login();
    cy.visit('/');
    cy.loadProject('Query test project')  
  })

  it('TC 13042 Open the Help page from contract overview page', () => {
    externalPersonnelPage.HelpButton().invoke('removeAttr', 'target').click()
    verifyHelpPage()
  });

  it('TC 13034 Open the Help page from contract detail page', () => {
    const contractNo = '312312341'
    cy.openContract(contractNo)
    
    /** open the 'General' tab */
    navigationDrawer.GeneralTab().click().invoke('attr', 'class').should('contain', 'isActive')
    contractDetailGeneralPage.HelpButton().invoke('removeAttr', 'target').click()
    
    verifyHelpPage()
  });

  it('TC 13082 Open the Help page from the create request sidesheet', () => {
    const contractNo = '312312341'
    cy.openContract(contractNo)
    
    /** open the 'Actual MPP' tab */
    navigationDrawer.ActualMPPTab().click().invoke('attr', 'class').should('contain', 'isActive')

    actualMppPage.AddMppRequestButton().click()

    createRequestSidesheet.CreateRequestSidesheet().should('be.visible')
    createRequestSidesheet.HelpButton().invoke('removeAttr', 'target').click()

    verifyHelpPage()
  });

  it('TC xxx Open the Help page from Active Requests page', () => {
    const contractNo = '312312341'
    cy.openContract(contractNo)
    
    /** open the 'Active Request' tab */
    navigationDrawer.ActiveRequestsTab().click().invoke('attr', 'class').should('contain', 'isActive')
    activeRequestPage.HelpButton().invoke('removeAttr', 'target').click()
    
    verifyHelpPage()
  });  

})