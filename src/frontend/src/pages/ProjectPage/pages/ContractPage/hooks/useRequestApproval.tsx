import { useState, useCallback } from 'react';
import { useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import { useAppContext } from '../../../../../appContext';
import { useContractContext } from '../../../../../contractContex';
import PersonnelRequest from '../../../../../models/PersonnelRequest';
import useEditableRequests from './useEditableRequests';

export default (requests: PersonnelRequest[], onApprove?: () => void) => {
    const [isApproving, setIsApproving] = useState<boolean>(false);
    const [approvedError, setApprovedError] = useState<Error | null>(null);
    const { apiClient } = useAppContext();
    const sendNotification = useNotificationCenter();
    const { canEdit, checkForEditAccessAsync } = useEditableRequests(requests, 'approve');

    const currentContext = useCurrentContext();
    const { contract, dispatchContractAction } = useContractContext();
    const projectId = currentContext?.externalId;
    const contractId = contract?.id;

    const approveRequestsAsync = useCallback(
        async (projectId: string, contractId: string, requests: PersonnelRequest[]) => {
            setApprovedError(null);
            setIsApproving(true);
            try {
                const responses = requests.map(
                    async request =>
                        await apiClient.approveRequestAsync(projectId, contractId, request.id)
                );
                const approvedRequests = await Promise.all(responses);
                const finishedRequest = approvedRequests.filter(
                    request => request.state === 'ApprovedByCompany'
                );
                const nonFinishedRequests = approvedRequests.filter(
                    request => request.state !== 'ApprovedByCompany'
                );
                if (finishedRequest.length > 0) {
                    dispatchContractAction({
                        verb: 'delete',
                        collection: 'activeRequests',
                        payload: finishedRequest,
                    });
                    dispatchContractAction({
                        verb: 'merge',
                        collection: 'completedRequests',
                        payload: finishedRequest,
                    });
                }
                if (nonFinishedRequests.length > 0) {
                    dispatchContractAction({
                        verb: 'merge',
                        collection: 'activeRequests',
                        payload: nonFinishedRequests,
                    });
                }

                sendNotification({
                    level: 'low',
                    title: `Request for ${requests
                        .map(r => r.position?.basePosition?.name || '')
                        .join(', ')} has been approved`,
                });
                await checkForEditAccessAsync(projectId, contractId, requests);
            } catch (e) {
                setApprovedError(e);
                sendNotification({
                    level: 'high',
                    title: 'Failed to approve request(s)',
                });
            }
            setIsApproving(false);
        },
        [apiClient]
    );

    const approve = useCallback(async () => {
        if (!canEdit || !projectId || !contractId || requests.length <= 0) {
            return;
        }

        const userApproval = await sendNotification({
            level: 'high',
            title: `Are you sure you want to approve ${requests.length} requests?`,
        });
        if (userApproval.confirmed) {
            await approveRequestsAsync(projectId, contractId, requests);
            onApprove  && onApprove()
        }
    }, [projectId, contractId, canEdit, requests, onApprove]);

    return { approve, canApprove: canEdit, isApproving, approvedError };
};
