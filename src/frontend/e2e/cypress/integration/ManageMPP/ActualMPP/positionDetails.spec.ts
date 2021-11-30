// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../../support" />

import PositionDetailsSidesheet from "../../../POM/PositionDetailsSidesheet"
const positionDetailsSidesheet = new PositionDetailsSidesheet()

import NavigationDrawer from "../../../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

describe('Actual MPP - Position Details', () => {
  /** TODO make login persistent between tests */
  before(() => {
    const contractNo = '312312341'
    
    cy.clearLocalStorage();
    cy.login();
    cy.visit('/');

    cy.loadProject('Query test project') 
    cy.openContract(contractNo)
  });

  it('TC 13087 Position Details', () => {
    navigationDrawer.ActualMPPTab().click().invoke('attr', 'class').should('contain', 'isActive')

    cy.wait(200)
    cy.get('[id="position-column"]', {timeout: 10*1000}).random().click()

    positionDetailsSidesheet.PositionDetailsSidesheet().should('be.visible')

    positionDetailsSidesheet.PositionDetailsSidesheet().within(() => {
        positionDetailsSidesheet.ProOrganisationTab().invoke('attr', 'class').should('contain', 'current')
        /** this line below failed then fetching a foreign object */
        //cy.get('[data-cy="pro-organisation-tab-content"]').should('not.contain', 'An error occurred')    
        
        positionDetailsSidesheet.PositionTimelineTab().click()
        positionDetailsSidesheet.PositionTimelineTabContent().should('not.contain', 'An error occurred') 

        positionDetailsSidesheet.ContractDisciplineNetworkTab().click()
        positionDetailsSidesheet.ContractDisciplineNetworkTabContent().should('not.contain', 'An error occurred')

        positionDetailsSidesheet.RoleDescriptionTab().click()
        positionDetailsSidesheet.RoleDescriptionTabContent().should('contain', 'Generic Role Description')

        positionDetailsSidesheet.CloseSidesheetButton().click()
    })
    
  });

})