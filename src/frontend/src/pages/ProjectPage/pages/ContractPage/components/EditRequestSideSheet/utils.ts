import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import { EditRequest } from '.';
import { v1 as uuid } from 'uuid';
import { Position } from '@equinor/fusion';
import CreatePersonnelRequest from '../../../../../../models/CreatePersonnelRequest';
import PersonnelRequestPosition from '../../../../../../models/PersonnelRequestPosition';
import Personnel from '../../../../../../models/Personnel';

export const transFormRequest = (
    personnelRequest: PersonnelRequest[] | null,
    taskOwners: Position[] | null
): EditRequest[] | null => {
    if (personnelRequest === null || personnelRequest.length === 0) {
        return null;
    }

    return personnelRequest.map(req => ({
        id: uuid(),
        requestId: req.id.length === 0 ? null : req.id,
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
        originalPositionId: req.originalPositionId,
    }));
};

const transformPositionToRequestPosition = (
    position: Position
): PersonnelRequestPosition | null => {
    const instance = position.instances[0];
    if (!instance) {
        return null;
    }

    return {
        appliesFrom: instance.appliesFrom,
        appliesTo: instance.appliesTo,
        basePosition: position.basePosition,
        name: position.name,
        workload: instance.workload,
        externalId: position.externalId,
        obs: instance.obs,
        taskOwner: { positionId: instance.parentPositionId },
    };
};

const transformPositionDetailsToPersonnel = (
    position: Position,
    personnel: Personnel[]
): Personnel | null => {
    const instance = position.instances[0];
    if (!instance) {
        return null;
    }
    return personnel.find(p => p.mail.toLowerCase() === instance.assignedPerson?.mail?.toLowerCase()) || null;
};

export const transformPositionsToChangeRequest = (
    positions: Position[],
    personnel: Personnel[]
): PersonnelRequest[] => {
    return positions.map<PersonnelRequest>(p => ({
        id: '',
        created: new Date(),
        state: 'Created',
        description: '',
        position: transformPositionToRequestPosition(p),
        person: transformPositionDetailsToPersonnel(p, personnel),
        contract: null,
        project: null,
        comments: [],
        originalPosition: transformPositionToRequestPosition(p),
        originalPositionId: p.id,
        originalPerson: transformPositionDetailsToPersonnel(p, personnel),
        createdBy: null,
        workflow: null,
        provisioningStatus: null,
        lastActivity: null,
    }));
};

export const transformPositionsToCopyRequest = (
    positions: Position[],
    personnel: Personnel[]
): PersonnelRequest[] => {
    return positions.map<PersonnelRequest>(p => ({
        id: '',
        created: new Date(),
        state: 'Created',
        description: '',
        position: transformPositionToRequestPosition(p),
        person: transformPositionDetailsToPersonnel(p, personnel),
        contract: null,
        project: null,
        comments: [],
        originalPosition: null,
        originalPositionId:null,
        originalPerson: null,
        createdBy: null,
        workflow: null,
        provisioningStatus: null,
        lastActivity: null,
    }));
};
const formatDateISOString = (date: Date | null): string | null => {
    if(!date) {
        return null
    }
    const isoDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000) //Converts to local date
        .toISOString()
        .split('T')[0];
    return isoDate;
};
export const transformToCreatePersonnelRequest = (req: EditRequest): CreatePersonnelRequest => ({
    id: req.requestId || undefined,
    description: req.description,
    person: {
        mail: req.person?.mail || '',
    },
    position: {
        appliesFrom: formatDateISOString(req.appliesFrom),
        appliesTo: formatDateISOString(req.appliesTo),
        basePosition: req.basePosition,
        id: req.positionId || null,
        name: req.positionName,
        obs: req.obs,
        workload: +req.workload.split('%')[0],
        taskOwner: req.taskOwner ? { positionId: req.taskOwner.id } : null,
    },
    originalPositionId: req.originalPositionId,
});

export const transformToCreatePersonnelRequests = (
    editRequests: EditRequest[]
): CreatePersonnelRequest[] => editRequests.map(transformToCreatePersonnelRequest);

export const createDefaultState = (originalPosition?: Position): EditRequest[] => [
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
        originalPositionId: originalPosition?.id || null,
    },
];

export const createCopyState = (request: EditRequest): EditRequest => ({
    ...request,
    id: uuid(),
    requestId: null,
    originalPositionId: null,
})