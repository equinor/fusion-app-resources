import { useState, useCallback } from 'react';
import { useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import { useAppContext } from '../../../../../appContext';
import { useContractContext } from '../../../../../contractContex';
import PersonnelRequest from '../../../../../models/PersonnelRequest';

export default (requests: PersonnelRequest[]) => {
    const [isDeleting, setIsDeleting] = useState<boolean>(false);
    const [deleteError, setDeleteError] = useState<Error | null>(null);
    const { apiClient } = useAppContext();
    const sendNotification = useNotificationCenter();
    const currentContext = useCurrentContext();
    const { contract, dispatchContractAction } = useContractContext();
    const projectId = currentContext?.externalId;
    const contractId = contract?.id;

    const deleteRequestsAsync = useCallback(
        async (projectId: string, contractId: string, requests: PersonnelRequest[]) => {
            setDeleteError(null);
            setIsDeleting(true);
            try {
                const responses = requests.map(
                    async request =>
                        await apiClient.deleteRequestAsync(projectId, contractId, request.id)
                );
                 await Promise.all(responses);

                dispatchContractAction({
                    verb: 'delete',
                    collection: 'activeRequests',
                    payload: requests,
                });

                sendNotification({
                    level: 'low',
                    title: `Request for ${requests
                        .map(r => r.position?.basePosition?.name || '')
                        .join(', ')} has been deleted`,
                });
            } catch (e) {
                setDeleteError(e);
                sendNotification({
                    level: 'high',
                    title: 'Failed to delete request(s)',
                });
            } finally {
                setIsDeleting(false);
            }
        },
        [apiClient, requests]
    );

    const deleteRequests = useCallback(async () => {
        if (!projectId || !contractId || requests.length <= 0) {
            return;
        }
        const userApproval = await sendNotification({
            level: 'high',
            title: `Are you sure you want to delete ${requests.length} requests?`,
        });
        if (userApproval.confirmed) {
            await deleteRequestsAsync(projectId, contractId, requests);
        }
    }, [projectId, contractId, requests]);

    return { deleteRequests, isDeleting, deleteError };
};
