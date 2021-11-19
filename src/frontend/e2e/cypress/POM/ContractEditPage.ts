/// <reference types="cypress" />

/**
 * Contract Edit pages
 */
 export default class ContractEditPage {
    CloseSidesheetButton(){
        return cy.get('#close-btn');
    }

    SaveButton(){
        return cy.get('#save-btn');
    }

    CancelButton(){
        return cy.get('#cancel-btn');
    }

    PreviousButton(){
        return cy.get('#previous-btn');
    }

    NextButton(){
        return cy.get('button').contains('Next');
    }

    SubmitButton(){
        return cy.get('#submit-btn');
    }

    ClearButton(){
        return cy.get('#clear-btn');
    }

    Step1Button(){
        return cy.get('a[class^="fc--Stepper__step]').first();
    }

    Step2Button(){
        return cy.get('a[class^="fc--Stepper__step]').eq(1);
    }

    ContractNameInputBox(){
        return cy.get('#contract-name-input');
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