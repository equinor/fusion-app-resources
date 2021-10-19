// ***********************************************
// This example commands.js shows you how to
// create various custom commands and overwrite
// existing commands.
//
// For more comprehensive examples of custom
// commands please read more here:
// https://on.cypress.io/custom-commands
// ***********************************************
//
//
// -- This is a parent command --
// Cypress.Commands.add('login', (email, password) => { ... })
//
//
// -- This is a child command --
// Cypress.Commands.add('drag', { prevSubject: 'element'}, (subject, options) => { ... })
//
//
// -- This is a dual command --
// Cypress.Commands.add('dismiss', { prevSubject: 'optional'}, (subject, options) => { ... })
//
//
// -- This will overwrite an existing command --
// Cypress.Commands.overwrite('visit', (originalFn, url, options) => { ... })


import * as faker from 'faker';

// TODO add interface for user
const acquireToken = (tenant: string): Cypress.Chainable<Cypress.Response<{}>> => {
  return cy.request({
    method: "POST",
    url: `https://login.microsoftonline.com/${tenant}/oauth2/v2.0/token`,
    form: true,
    body: {
      grant_type: 'client_credentials',
      client_id: Cypress.env('FUSION_CLIENT_ID'),
      client_secret: Cypress.env('FUSION_CLIENT_SECRET'),
      scope: `${Cypress.env('FUSION_RESOURCE')}/.default`,
      // resource: Cypress.env('FUSION_RESOURCE'),
    }
  });
}

/**
 * TODO - add person seed in fixtures
 * @param resource 
 * @returns 
 */
const processAuthResponse = (resource: string) => (response: Cypress.Response<{ access_token }>): void => {
  const token = response.body.access_token;
  faker.seed(0);
  const familyName = faker.name.lastName();
  const givenName = faker.name.firstName();
  const obj = {
    [`FUSION_AUTH_CACHE:${resource}:TOKEN`]: token,
    USER: {
      id: faker.datatype.uuid(),
      familyName,
      givenName,
      fullName: [givenName, familyName].join(' '),
      // TODO - make arg
      roles: ["ProView.Admin.DevOps"],
      upn: faker.internet.email
    }
  }
  localStorage.setItem('FUSION_AUTH_CACHE', JSON.stringify(obj));
}

Cypress.Commands.add('login', () => {
  const token = Cypress.env('TOKEN');
  if (token) {
    return localStorage.setItem('FUSION_AUTH_CACHE', JSON.stringify(token));
  }
  const tenant = Cypress.env('FUSION_TENANT_ID');
  const resource = Cypress.env('FUSION_RESOURCE');
  acquireToken(tenant).then(processAuthResponse(resource));
});

const API_KEY = '74b1613f-f22a-451b-a5c3-1c9391e91e68';

Cypress.Commands.add('fusion', () => cy.window().then((window: Window) => window[API_KEY]));

Cypress.Commands.add('currentUser', () => cy.fusion().then(async (x) => await x.auth.container.getCachedUserAsync()));

Cypress.Commands.add('loadProject', (name) => {
  cy.get('input[class^=fc--ContextSelector__searchInput]').should('be.visible').as('context-selector');
  // cy.get('[data-cy="context-selector"]').find('input').should('be.visible').as('context-selector');
  cy.get('@context-selector').type(name)
  cy.get("div[class^=fc--Menu__container]").contains(name, { timeout: 10000 }).click()
  // cy.get('[data-cy="context-selector-dropdown"]').contains(name, { timeout: 10000 }).click()
  cy.wait(3000)
});

Cypress.Commands.add('openContract', (number) => {
  cy.get('[data-cy="contract-id"]').contains(number).should('be.visible').as('contract-id')

  cy.get('@contract-id').invoke('attr', 'href').then(($href) => {
    const contractUrl = $href.toString().trim()
    cy.get('@contract-id').click()
    cy.intercept('GET', contractUrl).as('load-contract')
    cy.wait('@load-contract')
  })
});


