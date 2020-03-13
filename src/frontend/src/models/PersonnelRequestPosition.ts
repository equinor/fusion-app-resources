import { BasePosition } from '@equinor/fusion';

type PersonnelRequestPosition = {
    id?: string | null;
    externalId?: string | null;
    basePosition: BasePosition | null;
    name: string | null;
    appliesFrom: Date | null;
    appliesTo: Date | null;
    workload: number;
    obs?: string;
    taskOwner?: {
        id: string | null;
    };
};

export type PersonnelRequestBasePosition = {
    id: string;
    name: string | null;
    discipline: string | null;
    projectType: string | null;
    wasResolved: boolean | null;
};

export default PersonnelRequestPosition;
