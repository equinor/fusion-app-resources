
// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../../support" />

import ContractEditPage from "../../POM/ContractEditPage"
const contractEditPage = new ContractEditPage()

import ContractDetailGeneralPage from "../../POM/ContractDetailGeneralPage"
const contractDetail = new ContractDetailGeneralPage()

describe('Edit contract', () => {
  /** TODO make login persistent between tests */
  before(() => {
    cy.clearLocalStorage();
    cy.login();
    cy.visit('/');
  })

  beforeEach(function () {
    cy.fixture('ContractData.json').then((contractData) => {
      this.contractData = contractData
    })
  });

  it('TC 13041 Edit Contract', function () {
    const contractNo = '312312341'

    cy.loadProject('Query test project')
    
    cy.openContract(contractNo)
    
    contractDetail.EditContractButton().click()
    cy.contains('h2', 'Edit').should('be.visible')

    /** change the contract data*/
    cy.wait(1000);  /**  wait for rending */
    contractEditPage.ContractNameInputBox().clear().type(this.contractData.contractName)

    contractEditPage.CompanySelector().typeAndPick(this.contractData.companyName)

    contractEditPage.FromDatePicker().type(this.contractData.fromDate)
    contractEditPage.ToDatePicker().type(this.contractData.toDate)

    contractEditPage.EquinorContractRespPicker().typeAndPick(this.contractData.equinorContractResp)

    contractEditPage.EquinorCompanyRepPicker().typeAndPick(this.contractData.equinorCompanyRep)

    contractEditPage.NextButton().click()

    contractEditPage.ExternalCompanyRepPicker().typeAndPick(this.contractData.externalCompanyRep)
      
    contractEditPage.ExternalContractRespPicker().typeAndPick(this.contractData.externalContractResp)

    contractEditPage.SubmitButton().click()
    cy.wait(1500)

    /** verify changes are shown in contract details page */
    cy.contains('h2', this.contractData.contractName, {timeout: 15*1000})
    cy.contains('label', 'Contractor').next().should('contain', this.contractData.companyName)
    cy.contains('label', 'From date').next().should('contain', this.contractData.fromDate)
    cy.contains('label', 'To date').next().should('contain', this.contractData.toDate)

    contractDetail.EquinorCompanyRep().find('[class^="fc--PositionCard__positionName"]').should('contain', this.contractData.equinorCompanyRep)
    contractDetail.EquinorContractResp().find('[class^="fc--PositionCard__positionName"]').should('contain', this.contractData.equinorContractResp)
    contractDetail.ExternalCompanyRep().find('[class^="fc--PositionCard__positionName"]').should('contain', this.contractData.externalCompanyRep)
    contractDetail.ExternalContractResp().find('[class^="fc--PositionCard__positionName"]').should('contain', this.contractData.externalContractResp)
    
  });

})