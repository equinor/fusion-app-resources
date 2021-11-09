/** commands relates to delegate access, re-certify, and remove delegate */


/** search a keyword
 *  column: the column to search the keyword, eg. 'email', 'first-name', 'last-name', 'disciplines', 'phone', 'position', etc
 *  keyword: the keyword to search
*/
Cypress.Commands.add('searchText', (column, keyword) => {
    cy.get('[class^="fc--FilterPane__container"]').should('be.visible')
        .invoke('attr', 'class').should('not.contain', 'isCollapsed')

    cy.get('#search-bar').type(keyword)
    cy.wait(100)

    cy.collapseExpandSidesheets()

    cy.get('#contract-personnel-table').within(() => {
        /** each elements in the array should contains the keyword */
        cy.get('[id="' + column + '-column"]').each(function ($el, index, $list) {
            console.log($el, index, $list)
            expect($el).to.contain(keyword.trim())
        });
    });

    /** expand the sidesheets again */
    cy.collapseExpandSidesheets()
    cy.get('#search-bar').find('input').clear()
});

/** Disciplines filter
 *  item: the filter item to use, lists in the disciplines filter section, eg. 'Electro', 'IT', 'Aut/Inst/Tele', etc
*/
Cypress.Commands.add('disciplineFilter', (item) => {
    cy.get('[class^="fc--FilterPane__container"]').should('be.visible')
        .invoke('attr', 'class').should('not.contain', 'isCollapsed')
 
    cy.wait(100)
    cy.get('[id="filter"]').eq(1).as('displines-filter')  // cy.get('#disciplines-filter')
    
    cy.wait(500) /** wait for rending, bug: detected multiple renderers concurrently rendering the same context provider */
    cy.get('@displines-filter').contains('li', item.trim()).click()
    cy.get('@displines-filter').find('h4').should('contain', item.trim())
    cy.wait(1000)
    
    cy.collapseExpandSidesheets()
    
    cy.get('#contract-personnel-table').within(() => {
        /** TODO: add column id to each column in the data table !!! */
        cy.get('[id="disciplines-column"]').each(function ($el, index, $list) {
            console.log($el, index, $list)
            expect($el).to.contain(item.trim())
        });
    });

    cy.collapseExpandSidesheets() /** expand the sidesheets again */
    /** clear the filters */
    cy.get('@displines-filter').find('#reset-btn').click() /** wait for fusion-component filter pane to be merged */
})

/** AD Status filter
 *  item: the filter item to use, lists in AD status filter section, eg. 'Azure AD Approved', 'Azure AD pending approval', or 'No Azure Access'
*/
Cypress.Commands.add('adStatusFilter', (item) => {
    cy.get('[class^="fc--FilterPane__container"]').should('be.visible')
        .invoke('attr', 'class').should('not.contain', 'isCollapsed')

    //cy.get('#ad-status-filter')
    cy.get('[id="filter"]').eq(2).as('ad-filter')

    cy.wait(500) /** wait for rending, bug: detected multiple renderers concurrently rendering the same context provider */
    cy.get('@ad-filter').contains('li', item.trim()).click()
    cy.get('@ad-filter').find('h4').should('contain', item.trim())
    cy.wait(1000)

    cy.collapseExpandSidesheets()

    cy.get('#contract-personnel-table').within(() => {
        /** TODO: add column id to each column in the data table !!! */
        cy.get('[id="ad-column"]').each(function ($el, index, $list) {
            console.log($el, index, $list)
            if (item === 'Azure AD Approved')
                cy.wrap($el).find('div').should('have.id', 'approved')
            else if (item === 'Azure AD pending approval')
                cy.wrap($el).find('div').should('have.id', 'invite-sent')
            else
                cy.wrap($el).find('div').should('have.id', 'no-access')
        });
    });

    cy.collapseExpandSidesheets() /** expand the sidesheets again */
    cy.get('@ad-filter').find('#reset-btn').click() /** wait for fusion-component filter pane to be merged */
})