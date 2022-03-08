// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../../support" />

import NavigationDrawer from "../../../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

// import ContractPersonnelPage from "../../../POM/ContractPersonnelPage"
// const contractPersonnelPage = new ContractPersonnelPage()

import ActualMppPage from "../../../POM/ActualMppPage"
const actualMppPage = new ActualMppPage()

import CreateRequestSidesheet from "../../../POM/CreateRequestSidesheet"
const createrequestSidesheet = new CreateRequestSidesheet()

import RequestWorkFlow from "../../../POM/RequestWorkFlow"
const requestWorkFlow = new RequestWorkFlow()

import ActiveRequestPage from "../../../POM/ActiveRequestPage"
const activeRequestPage = new ActiveRequestPage()

import RequestDetailsSidesheet from "../../../POM/RequestDetailsSidesheet"
const requestDetailsSidesheet = new RequestDetailsSidesheet()

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

    it('TC xxx - Actual MPP - Create a new request', function () {
        navigationDrawer.ActualMPPTab().click().invoke('attr', 'class').should('contain', 'isActive')

        actualMppPage.AddRequestButton().click()

        cy.createRequest(this.requestData)

        navigationDrawer.ActiveRequestsTab().click().invoke('attr', 'class').should('contain', 'isActive')

        activeRequestPage.ActiveRequestTable().within(()=> {
            /** verify that the new request shows in the Active request table, normally in the first row */
            cy.get('#person-column').contains(this.requestData.assignedPerson)
            cy.get('#request-status-column').within(()=> {
                cy.checkRequestStatus('Approved', 'Pending', 'Pending', 'Pending')
            })
        })
    });
    
    it('approve a request', function () {
        /** select the newly created request */
        navigationDrawer.ActiveRequestsTab().click().invoke('attr', 'class').should('contain', 'isActive')

        activeRequestPage.ActiveRequestTable().within(()=> {
            cy.get('#selection-cell').click()
        })

        /** selecte the newly created request, click Approve button and confirm for the first time */
        activeRequestPage.ApproveRequestButton().click()

        /** confirm in the dialog box */
        // cy.get('#notification-dialog').find('div').should('contain', 'approve', 'request')
        // cy.get('#confirm-btn').click()
        cy.get('[class^="fc--Dialog__container"]').within(()=> {
            cy.get('[class^="fc--Dialog__dialogTitle"]').should('contain', 'approve', 'request')
            cy.contains('button', 'Ok').click()
        })

        /** verify the status is 2 green, 2 orange */
        activeRequestPage.ActiveRequestTable().within(()=> {
            /** check the newly rejected request, normally in the first row */
            cy.get('#person-column').contains(this.requestData.assignedPerson)
            cy.get('#request-status-column').within(()=> {
                cy.checkRequestStatus('Approved', 'Approved', 'Pending', 'Pending')
            })
        })

        /** selecte the newly created request, click Approve button and confirm for the second time */

        /** verify the status is 3 green, 1 orange. this might be tricky since it won't stay long in provisioning request table */

        /** verify that the request get approved and shown in completed and actual mpp talbe with 4 green icons. how long we should wait for provisioning? */
        
        

        /** delete the request for clean up */
        

    });

    it('TC xxx - Actual MPP - Edit a new request', () => {
        activeRequestPage.AddRequestButton().click()

        
    });
    
    it('TC xxx - Actual MPP - Remove a request from the Actual MPP table', () => {
        /** delete the request for clean up */
        

    });

})