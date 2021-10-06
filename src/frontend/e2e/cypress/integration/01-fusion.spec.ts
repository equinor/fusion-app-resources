
// type definitions for Cypress object "cy"
/// <reference types="cypress" />

// type definitions for custom commands like "createDefaultTodos"
/// <reference types="../support" />

describe('Fusion Basic Test', () => {
  /** TODO make login persistent between tests */
  before(() => {
    cy.clearLocalStorage();
    cy.login();
    cy.visit("/");
  });

  it('will load fusion', () => cy.fusion().then(fusion => expect(fusion).to.exist));
  it('has a user', () => cy.currentUser()
    .should('exist')
    .and(user => {
      expect(user).property('id');
      expect(user).property('upn');
    })
  );
  it('has a app wrapper', () => cy.get('#fusion-app').should('exist'));
})