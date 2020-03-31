import PersonnelRequest from '../../../../../models/PersonnelRequest';
import { useState, useCallback, useEffect } from 'react';
import { useCurrentContext } from '@equinor/fusion';
import { useContractContext } from '../../../../../contractContex';
import { useAppContext } from '../../../../../appContext';

export default (requests: PersonnelRequest[], action: 'approve' | 'reject') => {
    const [canEdit, setCanEdit] = useState<boolean>(false);
    const [editableRequestIds, setEditableRequestIds] = useState<string[]>([]);
    const { apiClient } = useAppContext();

    const currentContext = useCurrentContext();
    const { contract } = useContractContext();
    const projectId = currentContext?.externalId;
    const contractId = contract?.id;

    const checkForEditAccessAsync = useCallback(
        async (projectId: string, contractId: string, req: PersonnelRequest[]) => {
            if (req.some(r => !(r.state === 'Created' || r.state === 'SubmittedToCompany'))) {
                setCanEdit(false);
                return;
            }
            const responses = req.map(
                async request =>
                    await apiClient.canEditActionAsync(projectId, contractId, request.id, action)
            );
            const editStatuses = await Promise.all(responses);
            const allCanEdit = editStatuses.every(approval => approval);
            if (allCanEdit) {
                const newEditableRequestIds = [...editableRequestIds, ...requests.map(r => r.id)];
                const editable = requests.every(r => newEditableRequestIds.some(id => id === r.id));
                setEditableRequestIds(newEditableRequestIds);
                setCanEdit(editable);
            } else {
                setCanEdit(false);
            }
        },
        [apiClient, editableRequestIds, requests]
    );

    useEffect(() => {
        if (requests.length <= 0 || !projectId || !contractId) {
            setCanEdit(false);
            return;
        }

        const unCheckedRequest = requests.filter(r => !editableRequestIds.some(id => id === r.id));
        if (unCheckedRequest.length <= 0) {
            setCanEdit(true);
            return;
        }

        checkForEditAccessAsync(projectId, contractId, unCheckedRequest);
    }, [requests.length]);

    return { canEdit, checkForEditAccessAsync };
};
