import Personnel from './Personnel';
import Person from './Person';
import { ContractReference } from './contract';
import Project from './Project';
import Comment from './Comment';
import PersonnelRequestPosition from './PersonnelRequestPosition';
import Workflow from './Workflow';
import ProvisioningStatus from './ProvisioningStatus ';

export type RequestState =
    | 'Created'
    | 'SubmittedToCompany'
    | 'RejectedByContractor'
    | 'ApprovedByCompany'
    | 'RejectedByCompany';

type PersonnelRequest = {
    id: string;
    created: Date;
    updated?: Date;
    createdBy?: Person | null;
    updatedBy?: Person;
    state: RequestState;
    description: string;
    position: PersonnelRequestPosition | null;
    person: Personnel | null;
    contract: ContractReference | null;
    project: Project | null;
    comments: Comment[];
    workflow?: Workflow | null;
    provisioningStatus?: ProvisioningStatus | null;
    originalPerson: Personnel | null;
    originalPosition: PersonnelRequestPosition | null;
    originalPositionId: string | null;
    lastActivity: Date | null;
};

export default PersonnelRequest;
