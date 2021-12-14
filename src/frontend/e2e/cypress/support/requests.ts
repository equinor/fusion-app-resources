/**  type definitions for Cypress object "cy" */
/// <reference types="cypress" />

import CreateRequestSidesheet from "../POM/CreateRequestSidesheet"
const createrequestSidesheet = new CreateRequestSidesheet()

const projectId = '29ddab36-e7a9-418b-a9e4-8cfbc9591274'
const contractId = '5ca8efa6-eb36-4a15-8f09-0d92194713d7'

const token = Cypress.env('FUSION_TOKEN');  // only access token, without user info
const authorization = `Bearer ${token}`

/** create a request through GUI*/
Cypress.Commands.add('createRequest', (requestData) => {
    createrequestSidesheet.CreateRequestSidesheet().should('be.visible')

    /** load the person selector */
    cy.get('[id="assigned-person-input"]').click()
    cy.wait(2000)

    cy.get('[id="assigned-person-input"]').first().typeAndPick(requestData.assignedPerson)
    cy.get('[id="task-owner-input"]').first().typeAndPick(requestData.taskOwner)

    cy.fillTextInput(0, 'custom-position', requestData.customPositionTitle)

    cy.get('[id="applies-from-input"]').first().type(requestData.appliesFrom)
    cy.get('[id="applies-to-input"]').first().type(requestData.appliesTo)

    cy.fillTextInput(0, 'workload', requestData.workload)
    cy.fillTextInput(0, 'obs', requestData.obs)
    cy.fillTextInput(0, 'request-description', requestData.requestDescription)

    /** input base position in the end */
    cy.get('#base-position-input').find('input').typeAndPick(requestData.basePosition)

    createrequestSidesheet.SubmitButton().click()

    createrequestSidesheet.RequestProgressSidesheet().should('contain', 'Successful')
    createrequestSidesheet.RequestProgressSidesheet().find('#close-btn').click({force: true})
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
    cy.wait(500)

    switch (column) {
        case 'base-position':
        case 'assigned-person':
        case 'task-owner': 
            cy.get('[id="' + column + '-input"]').first().typeAndPick(data)
            break;
        case 'custom-position':
        case 'workload':
        case 'obs': 
        case 'request-description':
            cy.fillTextInput(0, column, data)
            break;
        case 'applies-from':
        case 'applies-to':
            cy.get('[id="' + column + '-input"]').first().clear().type(data)
            break;
        default:
            cy.log('No such input field')
            break;
    }

});


/** remove a selected request through GUI*/
Cypress.Commands.add('removeRequest', () => {
    cy.get('[class^="fc--Dialog__container"]').should('be.visible').within(() => {
        cy.contains('delete', 'request')

        cy.get('button').contains('Ok').click()
        cy.wait(1000)
    });
});

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