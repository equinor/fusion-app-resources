/// <reference types="cypress" />

/**
 * search and filters pane
 */
 export default class SearchFilterPane {
    CollapseExpandButton() {
        return cy.get('#collapse-expand-btn');
    }

    SearchBar() {
        return cy.get('#search-bar');
    }

    ResetButton() {
        return cy.get('#reset-btn')
    }

    DisciplinesFilter() {
        return cy.get('#disciplines-filter');
    }

    ADStatusFilter() {
        return cy.get('#ad-filter');
    }

    StatusFilter() {
        return cy.get('#request-status-filter');
    }
}