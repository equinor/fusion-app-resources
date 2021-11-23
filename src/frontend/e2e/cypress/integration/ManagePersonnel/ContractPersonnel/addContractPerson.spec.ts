// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../../support" />

import NavigationDrawer from "../../../POM/NavigationDrawer"
const navigationDrawer = new NavigationDrawer()

import ContractPersonnelPage from "../../../POM/ContractPersonnelPage"
const contractPersonnelPage = new ContractPersonnelPage()

import AddEditPersonSidesheet from "../../../POM/AddPersonSidesheet"
const addPersonSidesheet = new AddEditPersonSidesheet()

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
        navigationDrawer.ContractPersonnelTab().click().invoke('attr', 'class').should('contain', 'isActive')

        /** load contract person test data */
        cy.fixture('PersonData.json').then((personData) => {
          this.personData = personData
        })
    });

    it('TC 13060 - Add a new contract person', function () {
        cy.checkContractPersonExistence(this.personData[0].Email).then(i => {
            console.log('return value is: ', i)
            if (i == 0){ 
                cy.addContractPerson(this.personData[0].FirstName, this.personData[0].LastName, this.personData[0].Email, this.personData[0].PhoneNumber)  
            }

            else cy.log('the person with this email aleady exist')  
        });
        
        navigationDrawer.CloseContractButton().click({force:true})
    });

    it('TC 13063 - Edit selected person', function () {
        contractPersonnelPage.ContractPersonnelTable().within(() => {            
            cy.get('[id="email-column"]').each(($el, index, $list) => {
                console.log($el, index, $list)
                /** $el is a wrapped jQuery element  */
                if ($el.text() === this.personData[0].Email) {
                  console.log(index)
                  cy.get('[id="selection-cell"]').eq(index).click()
                } 
            })       
        });

        contractPersonnelPage.EditContractPersonButton().click()

        addPersonSidesheet.AddPersonSidesheet().should('be.visible').within(() => {
            cy.fillTextInput(0, 'first-name', this.personData[1].FirstName)
            addPersonSidesheet.SaveButton().should('not.have.class', 'disabled').click()           
        });

        /** in request progress sidesheet */ 
        addPersonSidesheet.RequestProgressSidesheet().should('be.visible').within(() => {
            cy.contains('Successful', {timeout: 20*1000}).should('be.visible')
            addPersonSidesheet.CloseSidesheetButton().click()
            cy.wait(100)
        });

        cy.collapseExpandSidesheets()       

        cy.get('[id="first-name-column"]').should('contain', this.personData[1].FirstName) 

        navigationDrawer.CloseContractButton().click({force:true})
    });

    it('TC 13062 - Delete selected person', function () {
        cy.deleteContractPerson(this.personData[0].Email)

        navigationDrawer.CloseContractButton().click({force:true})
    });

})