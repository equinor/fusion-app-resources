/** commands relates to delegate access, re-certify, and remove delegate */

import DelegateAccessSideSheet from "../POM/DelegateAccessSideSheet"
const delegateSidesheet = new DelegateAccessSideSheet()


/** Delegate admin access to new people
 *  responsible: 'external' or 'equinor'
 *  person: person name
 *  period: '1-month', '6-months', or '12-months'
*/
Cypress.Commands.add('delegateAdminAccess', (responsible, person, period) => {
    cy.get('[data-cy="' + responsible + '-resp-delegate-admin-access"]').as('delegate-access')
        .within(() => {
            cy.get('[data-cy="delegate-table"]').should('be.exist')

            cy.wait(100)
            cy.get('#delegate-btn').click()
            cy.wait(200)
        });

    /**  delegate sidesheet should show up */
    delegateSidesheet.DelegateSideSheet().should('be.visible').within(() => {
        cy.contains('Delegate access').should('be.visible')

        delegateSidesheet.DelegateButton().should('have.attr', 'disabled')

        cy.get('[data-cy="certify-' + period + '"]').click()

        delegateSidesheet.AddPeopleSelector().type(person)

        /** temperaly jump out from delegate sidesheet within */
        cy.wrap(Cypress.$('body')).within(() => {
            cy.get('[class^="fc--SearchableDropdown"]').contains(person).click()
        })

        cy.get('[data-cy="selected-person"]').should('contain', person)

        delegateSidesheet.DelegateButton().should('not.have.attr', 'disabled')
        delegateSidesheet.DelegateButton().click({ force: true })
        cy.wait(2000)
    });

    /** verify the updates in the delegate table */
    cy.get('@delegate-access').within(() => {
        cy.wait(100)
        cy.get('[id="delegated-to-person-column"]').last().should('contain', person)
        cy.get('[id="re-certification-date-column"]').last().should('contain', '-')
    });
});

/** Re-certify period to people already has admin access
 *  responsible: 'external' or 'equinor'
 *  personIndex: person index, start from 1
 *  period: '1-month', '6-months', or '12-months'
*/
Cypress.Commands.add('recertifyAdminAccess', (responsible, person, period) => {
    cy.get('[data-cy="' + responsible + '-resp-delegate-admin-access"]').as('delegate-access').within(() => {
        cy.get('[data-cy="delegate-table"]').should('be.exist')

        cy.getDelegateIndex('delegated-to-person-column', person).then(i => {
            console.log('return index is: ', i)
            cy.get('[id="selection-cell"]').eq(i).click()
        });

        cy.get('#recertify-btn').click()
        cy.wait(1000)
    });

    cy.get('[data-cy="recertify-popup"]').should('be.visible').within(() => {
        cy.get('[data-cy="certify-' + period + '"]').click()

        cy.get('#recertify-btn').click()
        cy.wait(2000)
    });

    cy.get('@delegate-access').within(() => {
         cy.getDelegateIndex('delegated-to-person-column', person).then(i => {
            console.log('return index is: ', i)
            cy.get('[id="re-certification-date-column"]').eq(i).should('not.contain', '-')
            cy.get('[id="selection-cell"]').eq(i).click() /** unselect the person */
        });
    });
});

/** Remove people already has admin access from the table
 *  responsible: 'external' or 'equinor'
 *  personIndex: person index, start from 1
*/
Cypress.Commands.add('removeAdminAccess', (responsible, person) => {
    cy.get('[data-cy="' + responsible + '-resp-delegate-admin-access"]').as('delegate-access').within(() => {
        cy.get('[data-cy="delegate-table"]').should('be.exist')

        cy.getDelegateIndex('delegated-to-person-column', person).then(i => {
            console.log('return index is: ', i)
            cy.get('[id="selection-cell"]').eq(i).click()
        });

        cy.wait(100)
        cy.get('#remove-btn').click()
        cy.wait(100)
    });

    cy.get('[class^="fc--Dialog__container"]').should('be.visible').within(() => {
        cy.contains('Remove delegated access')

        cy.get('button').contains('Yes').click()
        cy.wait(1000)
    });

    cy.get('@delegate-access').within(() => {
        cy.get('[id="delegated-to-person-column"]').should('not.contain', person)
    });

});


/** get a element with specific text, and return its index */
Cypress.Commands.add('getDelegateIndex', (id, person) => {
    cy.contains('[id="' + id + '"]', person).invoke('index').then((x) => {
        console.log('x is ', x)
        const y = ((x - 2) / 6) - 1;
        console.log('y is', y)
        cy.wrap(y)
    });
});
