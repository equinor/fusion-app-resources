import Personnel from './Personnel';
import Person from './Person';
import { ContractReference } from './contract';
import Project from './Project';
import Comment from './Comment';
import { Position } from '@equinor/fusion';

export enum RequestState {
    Created,
    Submitted,
    Approved,
    Rejected,
    Provisioned,
}

type PersonnelRequest = {
    id: string;
    created: Date;
    updated?: Date;
    createdBy: Person;
    updatedBy: Person;
    state: RequestState;
    description: string;
    position: Position | null;
    person: Personnel | null;
    contract: ContractReference | null;
    project: Project | null;
    comments: Comment[];
};

export default PersonnelRequest;
