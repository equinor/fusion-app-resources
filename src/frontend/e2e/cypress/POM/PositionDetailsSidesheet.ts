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

    ProOrganisationTabContent() {
        return cy.get('[data-cy="pro-organisation-tab-content"]')
    }

    PositionTimelineTab() {
        return cy.get('#position-timeline-tab')
    }

    PositionTimelineTabContent() {
        return cy.get('[data-cy="position-timeline-tab-content"]')
    }

    ContractDisciplineNetworkTab() {
        return cy.get('#contract-discipline-network-tab')
    }

    ContractDisciplineNetworkTabContent() {
        return cy.get('[data-cy="contract-discipline-network-tab-content"]')
    }

    RoleDescriptionTab() {
        return cy.get('#role-description-tab')
    }
    
    RoleDescriptionTabContent() {
        return cy.get('[data-cy="role-description-tab-content"]')
    }
    
    CloseSidesheetButton() {
        return cy.get('[id="close-btn"]');
    }
    
}