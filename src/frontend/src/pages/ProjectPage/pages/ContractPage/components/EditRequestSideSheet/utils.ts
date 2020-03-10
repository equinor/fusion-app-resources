import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import { EditRequest } from '.';
import { v1 as uuid } from 'uuid';
import { Position } from '@equinor/fusion';

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
        appliesFrom: req.position?.instances.find(i => i.appliesFrom)?.appliesFrom || null,
        appliesTo: req.position?.instances.find(i => i.appliesTo)?.appliesTo || null,
        basePosition: req.position?.basePosition || null,
        description: req.description,
        obs: req.position?.instances.find(i => i.obs)?.obs || '',
        positionName: req.position?.name || '',
        workload: req.position?.instances.find(i => i.workload)?.workload.toString() || '',
        person: req.person,
        parentPosition:
            parentPositions?.find(
                position =>
                    position.id ===
                        req.position?.instances.find(i => i.parentPositionId)?.parentPositionId ||
                    ''
            ) || null,
    }));
};

export const createDefaultState = (): EditRequest[] => [
    {
        id: uuid(),
        requestId: null,
        description: '',
        positionId: '',
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
