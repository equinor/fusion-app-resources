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

import ActiveRequestPage from "../../../POM/ActiveRequestPage"
const activeRequestPage = new ActiveRequestPage()

import RequestDetailsSidesheet from "../../../POM/RequestDetailsSidesheet"
const requestDetailsSidesheet = new RequestDetailsSidesheet()

describe('Active Requests', () => {
    /** TODO make login persistent between tests */
    before(() => {
        cy.clearLocalStorage();
        cy.login();
        cy.visit('/');

        /** before create request, check and add the person to the contract personnel table */
        
    });

    beforeEach(function () {
        const contractNo = '312312341'

        cy.loadProject('Query test project')
        cy.openContract(contractNo)

        /** open the 'Actual MPP' tab */
        navigationDrawer.ActiveRequestsTab().click().invoke('attr', 'class').should('contain', 'isActive')

        /** load the request data */
        cy.fixture('Request.json').then((requestData) => {
          this.requestData = requestData
        })
    });

    it.only('test', () => {
        //navigationDrawer.ActiveRequestsTab().click().invoke('attr', 'class').should('contain', 'isActive')

        // activeRequestPage.ActiveRequestTable().within(()=> {
        //     cy.get('#base-position-column').click()
        // })

        cy.wait(1000)
        cy.get('[id="base-position-column"]', {timeout: 10*1000}).first().click() // click doesn't work, why

        requestDetailsSidesheet.RequestDetailsSidesheet().should('be.visible')
        cy.wait(1000)

        cy.removeRequest()

    });
    
    it('TC 13086 - Active Requests - Create a new request', function () {
        activeRequestPage.AddRequestButton().click()

        cy.createRequest(this.requestData)
        
        /** verify that the new request shows in the Active request table */
        activeRequestPage.ActiveRequestTable().should('contain', this.requestData.assignedPerson)

        //navigationDrawer.CloseContractButton().click({force:true})
    });

    it('TC 13083 - Create a new actual MPP request based on the selected position', function () {
        
        
        navigationDrawer.CloseContractButton().click({force:true})
    });

    it('TC 13084 - Edit the request for the selected position', function () {
        
        
        navigationDrawer.CloseContractButton().click({force:true})
    });

    it('TC 23978 - Remove the request for the selected position', function () {
        
        
        navigationDrawer.CloseContractButton().click({force:true})
    });

})