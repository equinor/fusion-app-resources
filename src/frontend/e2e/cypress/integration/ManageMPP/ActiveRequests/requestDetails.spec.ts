// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../../support" />

import RequestDetailsSidesheet from "../../../POM/RequestDetailsSidesheet"
const requestDetailsSidesheet = new RequestDetailsSidesheet()

import NavigationDrawer from "../../../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

describe('Active Requests - Request Details', () => {
  /** TODO make login persistent between tests */
  before(() => {
    const contractNo = '312312341'
    
    cy.clearLocalStorage();
    cy.login();
    cy.visit('/');

    cy.loadProject('Query test project') 
    cy.openContract(contractNo)
  });

  it('TC 13091 - Request Details', () => {
    navigationDrawer.ActiveRequestsTab().click().invoke('attr', 'class').should('contain', 'isActive')

    cy.wait(200)
    cy.get('[id="base-position-column"]', {timeout: 10*1000}).random().click()

    requestDetailsSidesheet.RequestDetailsSidesheet().should('be.visible')

    requestDetailsSidesheet.RequestDetailsSidesheet().within(() => {
        requestDetailsSidesheet.RequestGeneralTab().invoke('attr', 'class').should('contain', 'current')
        requestDetailsSidesheet.RequestGeneralTabContent().should('contain', 'Description')   
        
        requestDetailsSidesheet.RequestDescriptionTab().click()
        requestDetailsSidesheet.RequestDescriptionTabContent().should('contain', 'Request description') 

        requestDetailsSidesheet.RequestPersonTab().click()
        requestDetailsSidesheet.RequestPersonTabContent().should('contain', 'First name')

        requestDetailsSidesheet.CloseSidesheetButton().click()
    })
  });
})