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
        return cy.get('[data-cy="contract-name"]');
    }

    CompanySelector(){
        return cy.get('[data-cy="company-picker"]');
    }

    FromDatePicker(){
        return cy.get('[data-cy="from-date-picker"]');
    }

    ToDatePicker(){
        return cy.get('[data-cy="to-date-picker"]');
    }

    EquinorContractRespPicker(){
        return cy.get('[data-cy="equinor-contract-resp"]');
    }

    EquinorCompanyRepPicker(){
        return cy.get('[data-cy="equinor-company-rep"]');
    }

    Step3Button(){
        return cy.get('a[class^="fc--Stepper__step]').last();
    }

    ExternalCompanyRepPicker(){
        return cy.get('[data-cy="external-company-rep"]').find('input');
    }

    ExternalContractRespPicker(){
        return cy.get('[data-cy="external-contract-resp"]').find('input');
    }
   
}