import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import { EditRequest } from '.';
import { v1 as uuid } from 'uuid';
import { Position } from '@equinor/fusion';
import CreatePersonnelRequest from '../../../../../../models/CreatePersonnelRequest';

export const transFormRequest = (
    personnelRequest: PersonnelRequest[] | null,
    parentPositions: Position[] | null
): EditRequest[] | null => {
    if (personnelRequest === null) {
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
        parentPosition:
            parentPositions?.find(position => position.id === req.position?.taskOwner?.positionId || '') ||
            null,
    }));
};

export const transformToCreatePersonnelRequest = (
    editRequests: EditRequest[]
): CreatePersonnelRequest[] => {
    return editRequests.map(req => {
        const personnel: CreatePersonnelRequest = {
            description: req.description,
            person: {
                mail: req.person?.mail || '',
            },
            position: {
                appliesFrom: req.appliesFrom,
                appliesTo: req.appliesTo,
                basePosition: req.basePosition?.id
                    ? {
                          id: req.basePosition.id,
                      }
                    : null,
                id: req.positionId || null,
                name: req.positionName,
                obs: req.obs,
                workload: +req.workload,
                taskOwner: req.parentPosition?.id
                    ? {
                          positionId: req.parentPosition.id,
                      }
                    : null,
            },
        };
        return personnel;
    });
};

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
        parentPosition: null,
    },
];
