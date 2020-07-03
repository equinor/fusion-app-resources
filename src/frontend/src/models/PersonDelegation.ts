import Person from './Person';
import Project from './Project';
import { ContractReference } from './contract';

export type PersonDelegationType = 'cr';

export type PersonDelegationClassification = 'external' | 'internal';

export type ReCertifyPersonDelegationRequest = {
    validTo: string;
};

export type PersonDelegationRequest = {
    type: PersonDelegationType;
    person: {
        azureUniquePersonId: string;
    };
    classification: PersonDelegationClassification;
    validTo: Date;
};

type PersonDelegation = {
    type: PersonDelegationType;
    person: Person;
    classification: PersonDelegationClassification;
    created: Date;
    createdBy: Person;
    validTo: Date;
    project: Project;
    contract: ContractReference;
    recertified: Date | null;
    recertifiedBy: PersonDelegationClassification | null;
};

export default PersonDelegation;
