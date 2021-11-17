import ContractPersonnelPage from "../POM/ContractPersonnelPage"
const contractPersonnelPage = new ContractPersonnelPage()

/** fill in person data
 * type: data type, eg. first-name, last-name, email, phone 
 * data: the data for the selected type
*/
Cypress.Commands.add('fillPersonData', (index, type, data) => {
    cy.get('[id="' + type + '-input"]').eq(index).find('input').clear().type(data)
});


/** delete person with specific email */
Cypress.Commands.add('deletePerson', (email) => {
    contractPersonnelPage.ContractPersonnelTable().within(() => {            
        cy.get('[id="email-column"]').each(($el, index, $list) => {
            console.log($el, index, $list)
            /** $el is a wrapped jQuery element  */
            if ($el.text() === email) {
              console.log(index)
              cy.get('[id="selection-cell"]').eq(index).click()
            } 
        })       
    });

    contractPersonnelPage.DeleteContractPersonButton().click()

    /**  confirm in dialog */
    cy.get('[class^="fc--Dialog__container"]').should('be.visible')
    .contains('button', 'sure').click() // cy.get('#confirm-btn').click()

    cy.get('[id="email-column"]').should('not.contain', email.trim())
});