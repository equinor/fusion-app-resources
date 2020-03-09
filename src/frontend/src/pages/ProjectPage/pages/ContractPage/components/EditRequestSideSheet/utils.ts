import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import { EditRequest } from '.';
import uuid = require('uuid');

export const transFormRequest = (
    personnelRequest: PersonnelRequest[] | null
): EditRequest[] | null => {
    if (personnelRequest === null) {
        return null;
    }

    return personnelRequest.map(req => ({
        id: uuid(),
        positionId: req.position?.id || '',
        appliesFrom: req.position?.instances.find(i => i.appliesFrom)?.appliesFrom || null,
        appliesTo: req.position?.instances.find(i => i.appliesTo)?.appliesTo || null,
        basePosition: req.position?.basePosition || null,
        description: req.description,
        obs: req.position?.instances.find(i => i.obs)?.obs || '',
        positionName: req.position?.name || '',
        workload: req.position?.instances.find(i => i.workload)?.workload.toString() || '',
        person: req.person,
    }));
};
