/** commands relates to delegate access, re-certify, and remove delegate */

import { el } from "date-fns/locale";


/** search a keyword
 *  column: the column to search the keyword, eg. 'email', 'first-name', 'last-name', 'disciplines', 'phone', 'position', etc
 *  keyword: the keyword to search
*/
Cypress.Commands.add('searchText', (column, keyword) => {
    cy.get('[class^="fc--FilterPane__container"]').should('be.visible')
        .invoke('attr', 'class').should('not.contain', 'isCollapsed')

    cy.get('#search-bar').type(keyword)
    cy.wait(100)

    cy.get('[class^="fc--DataTable__container"]').within(() => {
        /** TODO: add column id to each column in the data table !!! */
        cy.get('[id="' + column + '-column"]').each(function ($el, index, $list) {
            console.log($el, index, $list)
            expect($el).to.contain(keyword.trim())
        });
    });

    cy.get('#search-bar').clear()
});

/** Disciplines filter
 *  item: the filter item to use, lists in the disciplines filter section, eg. 'Electro', 'IT', 'Aut/Inst/Tele', etc
*/
Cypress.Commands.add('disciplineFilter', (item) => {
    cy.get('[class^="fc--FilterPane__container"]').should('be.visible')
        .invoke('attr', 'class').should('not.contain', 'isCollapsed')

    cy.get('#disciplines-filter').find('li').contains(item.trim()).click()
    cy.wait(100)

    cy.get('[class^="fc--DataTable__container"]').within(() => {
        /** TODO: add column id to each column in the data table !!! */
        cy.get('#disciplines-column').each(function ($el, index, $list) {
            console.log($el, index, $list)
            expect($el).to.contain(item.trim())
        });
    });

    cy.get('#disciplines-filter').find('#reset-btn').click() /** wait for fusion-component filter pane to be merged */
})

/** AD Status filter
 *  item: the filter item to use, lists in AD status filter section, eg. 'Azure AD Approved', 'Azure AD pending approval', or 'No Azure Access'
*/
Cypress.Commands.add('adStatusFilter', (item) => {
    cy.get('[class^="fc--FilterPane__container"]').should('be.visible')
        .invoke('attr', 'class').should('not.contain', 'isCollapsed')

    cy.get('#ad-status-filter').find('li').contains(item.trim()).click()
    cy.wait(100)

    cy.get('[class^="fc--DataTable__container"]').within(() => {
        /** TODO: add column id to each column in the data table !!! */
        cy.get('#ad-column').each(function ($el, index, $list) {
            console.log($el, index, $list)
            if (item === 'Azure AD Approved')
                cy.wrap($el).find('div').should('have.id', 'approved')
            else if (item === 'Azure AD pending approval')
                cy.wrap($el).find('div').should('have.id', 'invite-sent')
            else
                cy.wrap($el).find('div').should('have.id', 'no-access')
        });
    });

    cy.get('#ad-status-filter').find('#reset-btn').click() /** wait for fusion-component filter pane to be merged */
})