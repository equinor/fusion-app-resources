// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../../support" />

import NavigationDrawer from "../../../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

import CreateRequestSidesheet from "../../../POM/CreateRequestSidesheet"
const createrequestSidesheet = new CreateRequestSidesheet()

import ActiveRequestPage from "../../../POM/ActiveRequestPage"
const activeRequestPage = new ActiveRequestPage()

/** prerequirsites: the added the person should exist in the contract personnel table, and have Azure access */

describe('Active Requests', () => {
    /** TODO make login persistent between tests */
    before(() => {
        const contractNo = '312312341'
        
        cy.clearLocalStorage();
        cy.login();
        cy.visit('/');

        cy.loadProject('Query test project')
        cy.openContract(contractNo)       
    });

    beforeEach(function () {
        /** open the 'Actual MPP' tab */
        navigationDrawer.ActiveRequestsTab().click().invoke('attr', 'class').should('contain', 'isActive')

        /** load the request data */
        cy.fixture('Request.json').then((requestData) => {
          this.requestData = requestData
        })
    });

    
    it('TC 13086 - Active Requests - Create a new request', function () {
        activeRequestPage.AddRequestButton().click()

        cy.createRequest(this.requestData)
        
        /** verify that the new request shows in the Active request table */
        activeRequestPage.ActiveRequestTable().should('contain', this.requestData.assignedPerson)
    });


    it('TC 13084 - Active Requests - Edit the request for the selected position', () => {
        /** the new created request lists on the top in the table */
        cy.wait(1000)
        cy.get('#selection-cell').click()
        activeRequestPage.EditRequestButton().click()
        
        cy.editRequest('custom-position', 'tester')   
        cy.editRequest('applies-to', '31/12/2022')
        cy.editRequest('base-position', 'Estimator')

        createrequestSidesheet.SubmitButton().click()

        createrequestSidesheet.RequestProgressSidesheet().should('contain', 'Successful')
        createrequestSidesheet.RequestProgressSidesheet().find('#close-btn').click({force: true})

        /** verify data */ 
        cy.get('#custom-position-column').should('contain', 'tester')
        cy.get('#base-position-column').should('contain', 'Estimator')
    });

    it('TC 23978 - Active Requests - Remove the request for the selected position', () => {
        /** remove the first request on the top */
        cy.wait(1000)
        cy.get('#selection-cell').click() 
        activeRequestPage.RemoveRequestButton().click()

        cy.removeRequest()
    });

})