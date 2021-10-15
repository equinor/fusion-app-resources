/// <reference types="cypress" />

/**
 * Landing page of a project
 */
 export default class ContractEditPage {
    CloseButton(){
        return cy.get('[data-cy="close-btn"]');
    }

    SaveButton(){
        return cy.contains('Save');
    }

    CancelButton(){
        return cy.contains('Cancel');
    }

    PreviousButton(){
        return cy.contains('Previous');
    }

    NextButton(){
        return cy.contains('Next');
    }

    SubmitButton(){
        return cy.contains('Submit');
    }

    ClearButton(){
        return cy.contains('Clear');
    }

    Step1Button(){
        return cy.get('a[class^="fc--Stepper__step]').first();
    }

    ContractSelector(){
        return cy.get('[class^="fc--TextInput__inputContent]');
    }

    Step2Button(){
        return cy.get('a[class^="fc--Stepper__step]').eq(1);
    }

    ContractNameInputBox(){
        return cy.contains('Contract name');
    }

    CompanySelector(){
        return cy.get('[class^="fc--TextInput__inputContent]');
    }

    FromDatePicker(){
        return cy.contains('Allocate contract');
    }

    ToDatePicker(){
        return cy.contains('Cancel');
    }

    EquinorContractRespPicker(){
        return cy.contains('Cancel');
    }

    EquinorCompanyRepPicker(){
        return cy.contains('Cancel');
    }

    Step3Button(){
        return cy.get('a[class^="fc--Stepper__step]').last();
    }

    ExternalCompanyRepPicker(){
        return cy.contains('Cancel');
    }

    ExternalContractRespPicker(){
        return cy.contains('Cancel');
    }
   
}