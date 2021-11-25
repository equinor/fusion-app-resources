/// <reference types="cypress" />

//import { TestPerson } from "./model";

type Fusion = {
  auth: {
    container: {
      getCachedUserAsync: () => Promise<{}>
    }
  }
}

type PersonData = {
  firstName: string,
  lastName: string,
  email: string,
  phoneNumber: string,
}

type RequestData = {
  basePosition: string,
  assignedPerson: string,
  taskOwner?: string,
  customPositionTitle: string,
  appliesFrom: string,
  appliesTo: string,
  workload: string,
  oBS?: string,
  requestDescription?: string,
}

declare namespace Cypress {
  interface Chainable<Subject> {
    /** general functions */
    login(): Chainable<void>
    fusion(): Chainable<Fusion>
    currentUser(): Chainable<{ id: string, fullName: string }>
    loadProject(name: string): Chainable<void>
    openContract(number: string): Chainable<void>
    collapseExpandSidesheets(): Chainable<void>
    random(): Chainable<Element>
    
    /** fill in data */
    fillTextInput(index: number, column: string, data: string): Chainable<void>
    typeAndPick(data: string): Chainable<void>

    /** delegate admin access */
    getDelegateIndex(id: string, text: string): Chainable<number>

    /** search and filters */
    searchText(column: string, keyword: string): Chainable<void>
    disciplineFilter(item: string): Chainable<void>
    adStatusFilter(item: string): Chainable<void>

    /** contract person */
    checkContractPersonExistence(email: string): Chainable<number>
    addContractPerson(personData: PersonData): Chainable<void>
    deleteContractPerson(email: string): Chainable<void>

    /** download excel file */
    readExcelFile(filename: string): Chainable<string>

    /** request management */
    createRequest(requestData: RequestData): Chainable<void>

  }
}