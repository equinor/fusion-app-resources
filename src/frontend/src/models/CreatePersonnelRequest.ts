import { BasePosition } from '@equinor/fusion';

type CreatePersonnelRequest = {
    id?: string;
    description: string;
    position: CreatePersonnelRequestPosition | null;
    person: {
        mail: string;
        azureUniquePersonId: string;
    };
    originalPositionId: string | null;
};

type CreatePersonnelRequestPosition = {
    id: string | null;
    basePosition: BasePosition | null;
    name: string;
    appliesFrom: string | null;
    appliesTo: string | null;
    workload: number;
    obs: string;
    taskOwner: {
        positionId: string;
    } | null;
};

export default CreatePersonnelRequest;
