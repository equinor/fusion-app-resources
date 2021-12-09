/**  type definitions for Cypress object "cy" */
/// <reference types="cypress" />

import CreateRequestSidesheet from "../POM/CreateRequestSidesheet"
const createrequestSidesheet = new CreateRequestSidesheet()

const projectId = '29ddab36-e7a9-418b-a9e4-8cfbc9591274'
const contractId = '5ca8efa6-eb36-4a15-8f09-0d92194713d7'

// const token = Cypress.env('FUSION_AUTH_CACHE:5a842df8-3238-415d-b168-9f16a6a6031b:TOKEN');
const token = Cypress.env('FUSION_TOKEN');
const authorization = `Bearer ${token}`

/** create a request through GUI*/
Cypress.Commands.add('createRequest', (requestData) => {
    createrequestSidesheet.CreateRequestSidesheet().should('be.visible')

    //cy.wait(5000)

    //cy.get('#base-position-input').find('input').should('exist') // cannot find input, why?

    
    cy.get('[id="assigned-person-input"]').first().typeAndPick(requestData.assignedPerson)
    cy.get('[id="task-owner-input"]').first().typeAndPick(requestData.taskOwner)

    cy.fillTextInput(0, 'custom-position', requestData.customPositionTitle)

    cy.get('[id="applies-from-input"]').first().type(requestData.appliesFrom)
    cy.get('[id="applies-to-input"]').first().type(requestData.appliesTo)

    cy.fillTextInput(0, 'workload', requestData.workload)
    cy.fillTextInput(0, 'obs', requestData.obs)
    cy.fillTextInput(0, 'request-description', requestData.requestDescription)

    cy.get('#base-position-input').find('input').typeAndPick(requestData.basePosition)

    createrequestSidesheet.SubmitButton().click()

    createrequestSidesheet.RequestProgressSidesheet().should('contain', 'Successful')
    cy.get('#close-btn').click()
});

/** edit a selected request 
 * index: which row in the table to fill
 * column: which column in the table to fill
 * - position/person picker: base-position, assigned-person, task-owner 
 * - date picker: applies-from, applies-to
 * - text input: custom-position, workload, obs, request-description
 * data: the data to fill in
*/
Cypress.Commands.add('editRequest', (column, data) => {
    createrequestSidesheet.CreateRequestSidesheet().should('be.visible')

    if (column == 'base-position' || 'assigned-person' || 'task-owner')
        cy.get('[id="' + column + '-input"]').first().typeAndPick(data)

    if (column == 'custom-position' || 'workload' || 'obs' || 'request-description')
        cy.fillTextInput(0, column, data)

    if (column == 'applies-from' || 'applies-to')
        cy.get('[id="' + column + '-input"]').first().type(data)

    else
        cy.log('this input does not exist')

});


/** delete a selected request through GUI*/

/** approve a selected request 
 * 
*/

/** reject a selected request
 * 
 */

/** feed a request through API */
Cypress.Commands.add('feedRequest', () => {
    cy.fixture('RequestBody.json').then((requestData) => {
        cy.request('POST', 'projects/' + projectId + '/contracts/' + contractId + '/resources/requests?$expand=originalPosition', requestData)
        // .then((response)=> {
        //     expect(response.status).to.eq(200)  
        //     expect(response.body).to.have.property('id')
        //     cy.log('request id is', response.body.id)
        // })
    })
});


/** remove a request through API */
Cypress.Commands.add('removeRequest', () => {
    cy.url().then((requestUrl) => {
        cy.log("request url is", requestUrl)
        const requestId = requestUrl.substring(requestUrl.indexOf("=") + 1)
        cy.log("the request id is", requestId)

        const url = 'https://resources-api.ci.fusion-dev.net/projects/' + projectId + '/contracts/' + contractId + '/resources/requests/' + requestId + ''

        cy.log('url is', url)

        cy.request({
            method: 'DELETE', // GET for testing purpose
            url,
            headers: {
                authorization,
            }
        })
        .then((response) => {
            expect(response.status).to.eq(200)

        })
    })
});