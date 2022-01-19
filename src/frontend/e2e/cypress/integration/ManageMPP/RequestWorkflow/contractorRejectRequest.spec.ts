// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../../support" />

import NavigationDrawer from "../../../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

import ActiveRequestPage from "../../../POM/ActiveRequestPage"
const activeRequestPage = new ActiveRequestPage()

import RequestWorkFlow from "../../../POM/RequestWorkFlow"
const requestWorkFlow = new RequestWorkFlow()

import CompletedRequestPage from "../../../POM/CompletedRequestPage"
const completedRequestPage = new CompletedRequestPage()

import RequestDetailsSidesheet from "../../../POM/RequestDetailsSidesheet"
const requestDetailsSidesheet = new RequestDetailsSidesheet()

import RejectRequestSidesheet from "../../../POM/RejectRequestSidesheet"
const rejectRequestSidesheet = new RejectRequestSidesheet()

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

    
    it('TC 25345 - Request is rejected by a contractor at step 2', function () {
        cy.feedRequest()

        /** verify that request is created and shown in active request table */
        navigationDrawer.ActiveRequestsTab().click().invoke('attr', 'class').should('contain', 'isActive')

        activeRequestPage.ActiveRequestTable().within(()=> {
            /** selecte the newly created request, normally in the first row */
            cy.get('#person-column').contains('Qi Jin')
            requestWorkFlow.RequestStep2().should('have.attr', 'data-cy', 'Pending')
            cy.get('#selection-cell').click()
        })

        /** click Reject button */
        activeRequestPage.RejectRequestButton().should('not.have.class', 'disabled')
        activeRequestPage.RejectRequestButton().click()

        /** reject sidesheet shows up, fill in reject reson and confirm */
        rejectRequestSidesheet.RejectRequestSidesheet().should('be.visible')
        rejectRequestSidesheet.RejectReasonTextArea().type('testing')
        rejectRequestSidesheet.ConfrimRejectionButton().click()
        
        /** verify that the request get rejected and shown in completed talbe. */
        /** verify the status is 1 green, 1 red, and 2 grey */
        navigationDrawer.CompletedRequestsTab().click().invoke('attr', 'class').should('contain', 'isActive')
        completedRequestPage.CompletedRequestTable().within(()=> {
            /** check the newly rejected request, normally in the first row */
            cy.get('#person-column').contains('Qi Jin')
            requestWorkFlow.RequestStep2().should('have.attr', 'data-cy', 'Rejected')
            requestWorkFlow.RequestStep3().should('have.attr', 'data-cy', 'Skipped')
            requestWorkFlow.RequestStep4().should('have.attr', 'data-cy', 'Skipped')
        })
        
        /** delete the request for clean up */
        cy.get('#base-position-column').find('div').click() // not work

        requestDetailsSidesheet.RequestDetailsSidesheet().should('be.visible')
        cy.wait(1000)

        cy.clearRequest()

    });

})