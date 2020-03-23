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
    console.log(requests)
    const checkForEditAccessAsync = useCallback(
        async (projectId: string, contractId: string, req: PersonnelRequest[]) => {
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
            }
        },
        [apiClient, editableRequestIds]
    );

    useEffect(() => {
        const editable =
            requests.every(r => editableRequestIds.some(id => id === r.id)) && requests.length > 0;
        setCanEdit(editable);
    }, [requests]);

    useEffect(() => {
        if (requests.length <= 0 || !projectId || !contractId) {
            return;
        }
        const unCheckedRequest = requests.filter(r => !editableRequestIds.some(id => id === r.id));
        console.log("Unchecked", unCheckedRequest)
        if (unCheckedRequest.length <= 0) {
            return;
        }
        checkForEditAccessAsync(projectId, contractId, unCheckedRequest);
    }, [requests]);

    return { canEdit, checkForEditAccessAsync };
};
