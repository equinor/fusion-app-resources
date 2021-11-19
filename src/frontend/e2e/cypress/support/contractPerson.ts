import ContractPersonnelPage from "../POM/ContractPersonnelPage"
const contractPersonnelPage = new ContractPersonnelPage()

import AddEditPersonSidesheet from "../POM/AddPersonSidesheet"
const addPersonSidesheet = new AddEditPersonSidesheet()

/** fill in person data
 * type: data type, eg. first-name, last-name, email, phone 
 * data: the data for the selected type
*/
export const fillPersonData = (index: number, type: string, data: string) => {
    cy.get('[id="' + type + '-input"]').eq(index).find('input').clear().type(data)
}

/** Check the person whether exist in the contract person table*/
Cypress.Commands.add('checkContractPersonExistence', (email) => {
    contractPersonnelPage.ContractPersonnelTable().then(($table) => {
        if ($table.find('[id="email-column"]:contains('+email+')').length) { 
            console.log('the person with this email exist')
            cy.wrap(1)
        }

        else {
            console.log('the person with this email does not exist')
            cy.wrap(0)
        }
    })
});

/** add a new contract persion */
Cypress.Commands.add('addContractPerson', (firstName, lastName, email, phoneNumber) => {
    contractPersonnelPage.AddContractPersonButton().click()

    addPersonSidesheet.AddPersonSidesheet().should('be.visible').within(() => {
        fillPersonData(0, 'first-name', firstName)
        fillPersonData(0, 'last-name', lastName)
        fillPersonData(0, 'email', email)
        fillPersonData(0, 'phone', phoneNumber)

        addPersonSidesheet.SaveButton().should('not.have.class', 'disabled').click()
    });

    // in request progress sidesheet
    addPersonSidesheet.RequestProgressSidesheet().should('be.visible').within(() => {
        cy.contains('Successful', { timeout: 20 * 1000 }).should('be.visible')
        addPersonSidesheet.CloseSidesheetButton().click()
        cy.wait(100)
    });

    cy.get('[id="email-column"]').should('contain', email.trim())
})


/** delete contract person with specific email */
Cypress.Commands.add('deleteContractPerson', (email) => {
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