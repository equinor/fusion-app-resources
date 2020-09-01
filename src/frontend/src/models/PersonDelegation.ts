import Person from './Person';
import Project from './Project';
import { ContractReference } from './contract';

export type PersonDelegationType = 'CR';

export type PersonDelegationClassification = 'External' | 'Internal';

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
    id: string;
    type: PersonDelegationType;
    person: Person;
    classification: PersonDelegationClassification;
    created: Date;
    createdBy: Person;
    validTo: Date;
    project: Project;
    contract: ContractReference;
    recertifiedDate: Date | null;
    recertifiedBy: Person | null;
};

export default PersonDelegation;
