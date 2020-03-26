import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import { EditRequest } from '.';
import { v1 as uuid } from 'uuid';
import { Position } from '@equinor/fusion';
import CreatePersonnelRequest from '../../../../../../models/CreatePersonnelRequest';

export const transFormRequest = (
    personnelRequest: PersonnelRequest[] | null,
    taskOwners: Position[] | null
): EditRequest[] | null => {
    if (personnelRequest === null || personnelRequest.length === 0) {
        return null;
    }

    return personnelRequest.map(req => ({
        id: uuid(),
        requestId: req.id,
        positionId: req.position?.id || '',
        appliesFrom: req.position?.appliesFrom || null,
        appliesTo: req.position?.appliesTo || null,
        basePosition: req.position?.basePosition || null,
        description: req.description,
        obs: req.position?.obs || '',
        positionName: req.position?.name || '',
        workload: req.position?.workload.toString() || '',
        person: req.person,
        taskOwner:
            taskOwners?.find(
                position => position.id === req.position?.taskOwner?.positionId || ''
            ) || null,
    }));
};

export const transformToCreatePersonnelRequest = (req: EditRequest): CreatePersonnelRequest => ({
    id: req.requestId || undefined,
    description: req.description,
    person: {
        mail: req.person?.mail || '',
    },
    position: {
        appliesFrom: req.appliesFrom,
        appliesTo: req.appliesTo,
        basePosition: req.basePosition,
        id: req.positionId || null,
        name: req.positionName,
        obs: req.obs,
        workload: +req.workload,
        taskOwner: req.taskOwner ? { positionId: req.taskOwner.id } : null,
    },
});

export const transformToCreatePersonnelRequests = (
    editRequests: EditRequest[]
): CreatePersonnelRequest[] => editRequests.map(transformToCreatePersonnelRequest);

export const createDefaultState = (): EditRequest[] => [
    {
        id: uuid(),
        requestId: null,
        description: '',
        positionId: null,
        basePosition: null,
        positionName: '',
        appliesFrom: null,
        appliesTo: null,
        workload: '',
        obs: '',
        person: null,
        taskOwner: null,
    },
];
