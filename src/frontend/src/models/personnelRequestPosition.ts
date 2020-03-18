import { BasePosition } from '@equinor/fusion';

type PersonnelRequestPosition = {
    id: string | null; 
    basePosition: BasePosition | null;
    name: string;
    appliesFrom: Date | null;
    appliesTo: Date | null;
    workload: number;
    obs: string;
    taskOwner: {
        positionId: string
    } | null
};

export default PersonnelRequestPosition;