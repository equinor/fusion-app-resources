/** commands relates to delegate access, re-certify, and remove delegate */


/** search a keyword
 *  column: the column to search the keyword
 *  keyword: the keyword to search
*/
Cypress.Commands.add('searchInFilterPane', (column, keyword) => {
    cy.get('[class^="fc--FilterPane__container"]').should('be.visible')
    .invoke('attr', 'class').should('not.contain', 'isCollapsed')

    cy.get('#search-filter').type(keyword) 
    cy.wait(100)

    cy.get('[class^="fc--DataTable__container"]').within(() => {
        /** TODO: add column id to each column in the data table !!! */
        cy.get('[id="'+column+'"]').each(function($el, index, $list){
            console.log($el, index, $list)
            expect($el).to.contain(keyword)           
        });
    });

    cy.get('#search-filter').clear()
});

/** use a filter
 *  filter: the filter section to use
 *  item: the filter item to use
*/
Cypress.Commands.add('filter', (filter, item) => {
    cy.get('[class^="fc--FilterPane__container"]').should('be.visible')
    .invoke('attr', 'class').should('not.contain', 'isCollapsed')

    cy.get('[id="'+filter+'-filter"]').as('filter')
    cy.get('@filter').find('li').contains(item).click()
    cy.wait(100)

    cy.get('[class^="fc--DataTable__container"]').within(() => {
        /** TODO: add column id to each column in the data table !!! */
        cy.get('[id="'+filter+'"]').each(function($el, index, $list){
            console.log($el, index, $list)
            expect($el).to.contain(item)           
        });
    });

    cy.get('@filter').find('#reset-btn').click() /** wait for fusion-component filter pane to be merged */
})