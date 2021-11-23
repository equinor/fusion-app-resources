/// <reference types="cypress" />

/**
 * Position Details Sidesheet
 */
 export default class PositionDetailsSidesheet {
    PositionDetailsSidesheet() {
        return cy.get('#position-details-sidesheet');
    }

    ProOrganisationTab() {
        return cy.get('#pro-organisation-tab')
    }

    PositionTimelineTab() {
        return cy.get('#position-timeline-tab')
    }

    ContractDisciplineNetworkTab() {
        return cy.get('#contract-discipline-network-tab')
    }

    RoleDescriptionTab() {
        return cy.get('#role-description-tab')
    }
    
    CloseSidesheetButton() {
        return cy.get('[id="close-btn"]');
    }
    
}