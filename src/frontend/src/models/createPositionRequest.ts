import { BasePosition } from '@equinor/fusion';

export type AssignedPerson = {
    azureUniquePersonId: string | null;
    mail: string | null;
};

type CreatePositionRequest = {
    basePosition: BasePosition | null;
    name: string;
    appliesFrom: Date | null;
    appliesTo: Date | null;
    assignedPerson: AssignedPerson | null;
    workload: number;
};

export default CreatePositionRequest;
