/** commands relates to delegate access, re-certify, and remove delegate */

import DelegateAccessSideSheet from "../POM/DelegateAccessSideSheet"
const delegateSidesheet = new DelegateAccessSideSheet()


/** Delegate admin access to new people
 *  responsible: 'external' or 'equinor'
 *  person: person name
 *  period: '1-month', '6-months', or '12-months'
*/
Cypress.Commands.add('delegateAdminAccess', (responsible, person, period) => {
    cy.get('[data-cy="'+responsible+'-resp-delegate-admin-access"]').as('delegate-access').within(() => {
        cy.get('[data-cy="delegate-table"]').should('be.exist')
        
        cy.wait(100)
        cy.get('button').contains('Delegate').click()
        cy.wait(200)
    });

    /**  delegate sidesheet should show up */
    delegateSidesheet.DelegateSideSheet().should('be.visible').within(() => {
        cy.contains('Delegate access').should('be.visible')

        delegateSidesheet.DelegateButton().should('have.attr', 'disabled')

        delegateSidesheet.DelegateSideSheet().find('[data-cy="certify-'+period+'"]').click()
    
        delegateSidesheet.AddPeopleSelector().type(person)
        cy.get('[class^="fc--SearchableDropdown"]').contains(person).click()
        cy.get('[data-cy="selected-person"]').should('contain', person)

        delegateSidesheet.DelegateButton().should('not.have.attr', 'disabled')
        delegateSidesheet.DelegateButton().click({force: true})
        cy.wait(1000)
    });

    /** verify the updates in the delegate table */ 
    cy.get('@delegate-access').within(() => {
        cy.get('[data-cy="assigned-person"]').should('contain', person)
        cy.get('[data-cy="recertification-date"]').last().should('contain', '-')
    });
});

/** Re-certify period to people already has admin access
 *  responsible: 'external' or 'equinor'
 *  personIndex: person index, start from 1
 *  period: '1-month', '6-months', or '12-months'
*/
Cypress.Commands.add('recertifyAdminAccess', (responsible, personIndex, period) => {
    cy.get('[data-cy="'+responsible+'-resp-delegate-admin-access"]').as('delegate-access').within(() => {
        cy.get('[data-cy="delegate-table"]').should('be.exist')

        cy.get('input[type="checkbox"]').eq(personIndex).click()

        cy.get('button').contains('Re-certify').click()
        cy.wait(1000)
    });

    cy.get('[data-cy="recertify-popup"]').should('be.visible').within(() => {
        cy.get('[data-cy="certify-'+period+'"]').click()

        cy.get('[data-cy="recertify-btn"]').click()
        cy.wait(1000)
    });

    cy.get('@delegate-access').within(() => {
        cy.get('[data-cy="recertification-date"]').eq(personIndex-1).should('not.contain', '-')

        cy.get('input[type="checkbox"]').eq(personIndex).click() /** unselect the person */
    });
});

/** Remove people already has admin access from the table
 *  responsible: 'external' or 'equinor'
 *  personIndex: person index, start from 1
*/
Cypress.Commands.add('removeAdminAccess', (responsible, personIndex) => {
    cy.get('[data-cy="'+responsible+'-resp-delegate-admin-access"]').as('delegate-access').within(() => {
        cy.get('[data-cy="delegate-table"]').should('be.exist')

        cy.get('input[type="checkbox"]').eq(personIndex).click()

        cy.wait(100)
        cy.get('button').contains('Remove').click()
        cy.wait(100)
    });

    cy.get('[class^="fc--Dialog__container"]').should('be.visible').within(() => {
        cy.contains('Remove delegated access')

        cy.get('button').contains('Yes').click()
        cy.wait(1000)
    });

    cy.get('@delegate-access').within(() => {
        cy.get('[data-cy="assigned-person"]').its('length').should('eq', personIndex-1)

        cy.get('input[type="checkbox"]').eq(personIndex).click() /** unselect the person */
    });
    
});