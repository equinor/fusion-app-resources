import { BasePosition } from '@equinor/fusion';
import { AssignedPerson } from './createPositionRequest';

type CreatePersonnelRequest = {
    id: string;
    description: string;
    position: PersonnelRequestPosition | null;
    person: AssignedPerson | null;
};

type PersonnelRequestPosition = {
    id: string;
    basePosition: BasePosition | null;
    name: string;
    appliesFrom: Date | null;
    appliesTo: Date | null;
    workload: number;
    obs: string;
    taskOwner: PersonnelRequestPosition | null;
};

export default CreatePersonnelRequest;
