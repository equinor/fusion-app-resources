/** commands relates to delegate access, re-certify, and remove delegate */
import NavigationDrawer from "../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

import SearchFilterPane from "../POM/SearchAndFiltersPane"
const searchFilterPane = new SearchFilterPane()

import RequestWorkFlow from "../POM/RequestWorkFlow"
const requestWorkFlow = new RequestWorkFlow()

import ContractPersonnelPage from "../POM/contractPersonnelPage"
const contractPersonnelPage = new ContractPersonnelPage()

/** search a keyword
 *  column: the column to search the keyword, eg. 'email', 'first-name', 'last-name', 'disciplines', 'phone', 'position', etc
 *  keyword: the keyword to search
*/
Cypress.Commands.add('searchText', (column, keyword) => {
    cy.get('[class^="fc--FilterPane__container"]').should('be.visible')
        .invoke('attr', 'class').should('not.contain', 'isCollapsed')

    cy.wait(500)
    searchFilterPane.SearchBar().type(keyword)

    cy.viewport(1920, 1080)

    /** each elements in the array should contains the keyword */
    cy.get('[id="' + column + '-column"]', {timeout: 10000}).each(function ($el, index, $list) {
        console.log($el, index, $list)
        expect($el).to.contain(keyword.trim())
    });

    searchFilterPane.SearchBar().find('input').clear()
});

/** Disciplines filter
 *  item: the filter item to use, lists in the disciplines filter section, eg. 'Electro', 'IT', 'Aut/Inst/Tele', etc
*/
Cypress.Commands.add('disciplineFilter', (item) => {
    cy.get('[class^="fc--FilterPane__container"]').should('be.visible')
        .invoke('attr', 'class').should('not.contain', 'isCollapsed')

    cy.wait(100)
    cy.get('[id="disciplines-filter"]').as('displines-filter')

    cy.wait(500) /** wait for rending, bug: detected multiple renderers concurrently rendering the same context provider */
    cy.get('@displines-filter').contains('li', item.trim()).click()
    cy.get('@displines-filter').find('h4').should('contain', item.trim())
    cy.wait(1000)

    cy.viewport(1920, 1080)

    cy.get('[id="disciplines-column"]', {timeout: 10000}).each(function ($el, index, $list) {
        console.log($el, index, $list)
        expect($el).to.contain(item.trim())
    });

    /** clear the filters */
    cy.get('@displines-filter').find('#reset-btn').click()
})

/** AD Status filter
 *  item: the filter item to use, lists in AD status filter section, eg. 'Azure AD Approved', 'Azure AD pending approval', or 'No Azure Access'
*/
Cypress.Commands.add('adStatusFilter', (item) => {
    cy.get('[class^="fc--FilterPane__container"]').should('be.visible')
        .invoke('attr', 'class').should('not.contain', 'isCollapsed')

    cy.get('[id="ad-status-filter"]').as('ad-filter')

    cy.wait(500) /** wait for rending, bug: detected multiple renderers concurrently rendering the same context provider */
    cy.get('@ad-filter').contains('li', item.trim()).click()
    cy.get('@ad-filter').find('h4').should('contain', item.trim())
    cy.wait(1000)

    cy.viewport(1920, 1080)

    contractPersonnelPage.ContractPersonnelTable().within(() => {
        cy.get('[id="ad-column"]').each(function ($el, index, $list) {
            console.log($el, index, $list)
            if (item === 'Azure AD Approved')
                cy.wrap($el).find('div').should('have.id', 'approved')
            else if (item === 'Azure AD pending approval')
                cy.wrap($el).find('div').should('have.id', 'invite-sent')
            else if (item === 'No Azure Access')
                cy.wrap($el).find('div').should('have.id', 'no-access')
            else
                cy.log('the filter does not exist')
        });
    });

    cy.get('@ad-filter').find('#reset-btn').click()
})


/** Request Status filter
 *  item: the filter item to use, lists in the request status filter section, 
 *  eg. 'Created', 'SubmittedToCompany', 'RejectedByContractor', 'ApprovedByCompany', or 'RejectedByCompany'
*/
Cypress.Commands.add('requestStatusFilter', (item) => {
    cy.get('[class^="fc--FilterPane__container"]').should('be.visible')
        .invoke('attr', 'class').should('not.contain', 'isCollapsed')

    cy.get('[id="request-status-filter"]').as('request-status-filter')

    cy.wait(500) /** wait for rending, bug: detected multiple renderers concurrently rendering the same context provider */
    cy.get('@request-status-filter').contains('li', item.trim()).click()
    cy.get('@request-status-filter').find('h4').should('contain', item.trim())
    cy.wait(1000)

    cy.viewport(1920, 1080)

    cy.get('[id="request-status-column"]', {timeout: 10000}).each(function ($el, index, $list) {
        console.log($el, index, $list)
        if (item === 'Created')
            cy.wrap($el).within(() => {
                requestWorkFlow.RequestStep1().should('have.attr', 'data-cy', 'Approved')
                requestWorkFlow.RequestStep2().should('have.attr', 'data-cy', 'Pending')
            })
            
        else if (item === 'SubmittedToCompany')
            cy.wrap($el).within(() => {
                requestWorkFlow.RequestStep2().should('have.attr', 'data-cy', 'Approved')
                requestWorkFlow.RequestStep3().should('have.attr', 'data-cy', 'Pending')
            })
            
        else if (item === 'RejectedByContractor')
            cy.wrap($el).within(() => {
                requestWorkFlow.RequestStep2().should('have.attr', 'data-cy', 'Rejected')
            })
        

        else if (item === 'ApprovedByCompany')
            cy.wrap($el).within(() => {
                requestWorkFlow.RequestStep3().should('have.attr', 'data-cy', 'Approved')
            })    

        else if (item === 'RejectedByCompany')
            cy.wrap($el).within(() => {
                requestWorkFlow.RequestStep3().should('have.attr', 'data-cy', 'Rejected')
            })

        else
            cy.log('the filter does not exist')
    });

    cy.get('@request-status-filter').find('#reset-btn').click()
})