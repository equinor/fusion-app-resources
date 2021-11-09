// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../support" />

describe('Contract Personnel', () => {
    /** TODO make login persistent between tests */
    before(() => {
        cy.clearLocalStorage();
        cy.login();
        cy.visit('/');
    });

    beforeEach(function () {
        const contractNo = '312312341'

        cy.loadProject('Query test project')
        cy.openContract(contractNo)
        /** open the 'contract personnel' tab */
        cy.get('#contract-personnel-tab').click().invoke('attr', 'class').should('contain', 'isActive')

        /** load contract person test data */
        cy.fixture('PersonData.json').then((personData) => {
          this.personData = personData
        })
    });

    it('TC 13060 - Add a new person', function () {
        cy.get('#add-btn').click()

        cy.get('#add-person-sidesheet').should('be.visible').within(() => {
            cy.fillPersonData(0, 'first-name', this.personData.FirstName1)
            cy.fillPersonData(0, 'last-name', this.personData.LastName1)
            cy.fillPersonData(0, 'email', this.personData.Email)
            cy.fillPersonData(0, 'phone', this.personData.PhoneNumber1)

            cy.get('#save-btn').should('not.have.class', 'disabled').click()            
        });

        // in request progress sidesheet
        cy.get('#add-personnel-request-progress-sidesheet').should('be.visible').within(() => {
            cy.contains('Successful', {timeout: 20*1000}).should('be.visible')
            cy.get('#close-btn').click()
            cy.wait(100)
        });

        cy.get('[id="email-column"]').should('contain', this.personData.Email.trim())

        cy.get('#close-contract-btn').click({force:true})
    });

    it('TC 13063 - Edit selected person', function () {
        cy.get('#contract-personnel-table').within(() => {            
            cy.get('[id="email-column"]').each(($el, index, $list) => {
                console.log($el, index, $list)
                /** $el is a wrapped jQuery element  */
                if ($el.text() === this.personData.Email) {
                  console.log(index)
                  cy.get('[id="selection-cell"]').eq(index).click()
                } 
            })       
        });

        cy.get('#edit-btn').click()

        cy.get('#add-person-sidesheet').should('be.visible').within(() => {
            cy.fillPersonData(0, 'first-name', this.personData.FirstName2)
            cy.get('#save-btn').should('not.have.class', 'disabled').click()           
        });

        /** in request progress sidesheet */ 
        cy.get('#add-personnel-request-progress-sidesheet').should('be.visible').within(() => {
            cy.contains('Successful', {timeout: 20*1000}).should('be.visible')
            cy.get('#close-btn').click()
            cy.wait(100)
        });

        /** collapse both navigation sidesheet and filter sidesheet to make sure the first name column and the phone number column show up */
        // cy.get('#resources-contract-navigation-drawer').find('#collapse-expand-btn').click()
        // cy.get('filter-pane').find('#collapse-expand-btn').click()
        cy.get('[class^="fc--NavigationDrawer__collapseButtonContainer"]').find('button').click()
        cy.get('[class^="fc--FilterPane__collapseExpandButtonContainer"]').find('button').click()        

        cy.get('[id="first-name-column"]').should('contain', this.personData.FirstName2) 

        cy.get('#close-contract-btn').click({force:true})
    });

    it('TC 13062 - Delete selected person', function () {
        cy.get('#contract-personnel-table').within(() => {            
            cy.get('[id="email-column"]').each(($el, index, $list) => {
                console.log($el, index, $list)
                /** $el is a wrapped jQuery element  */
                if ($el.text() === this.personData.Email) {
                  console.log(index)
                  cy.get('[id="selection-cell"]').eq(index).click()
                } 
            })       
        });

        cy.get('#delete-btn').click()

        /**  confirm in dialog */
        cy.get('[class^="fc--Dialog__container"]').should('be.visible')
        .contains('button', 'sure').click() // cy.get('#confirm-btn').click()

        cy.get('[id="email-column"]').should('not.contain', this.personData.Email.trim())

        cy.get('#close-contract-btn').click()
    });

})