import { BasePosition } from '@equinor/fusion';

type PersonnelRequestPosition = {
    id: string | null;
    externalId: string | null;
    basePosition: BasePosition | null;
    name: string;
    appliesFrom: Date | null;
    appliesTo: Date | null;
    workload: number;
    obs?: string;
    taskOwner: {
        id: string;
    } | null;
};

export default PersonnelRequestPosition;
