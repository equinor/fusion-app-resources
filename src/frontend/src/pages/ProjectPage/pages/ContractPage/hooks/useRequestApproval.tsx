import * as React from 'react';
import { useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import { useAppContext } from '../../../../../appContext';
import { useContractContext } from '../../../../../contractContex';
import PersonnelRequest from '../../../../../models/PersonnelRequest';
import useEditableRequests from './useEditableRequests';

export default (requests: PersonnelRequest[]) => {
    const [isApproving, setIsApproving] = React.useState<boolean>(false);
    const [approvedError, setApprovedError] = React.useState<Error | null>(null);
    const { apiClient } = useAppContext();
    const sendNotification = useNotificationCenter();
    const { canEdit, checkForEditAccessAsync } = useEditableRequests(requests, 'approve');

    const currentContext = useCurrentContext();
    const { contract, dispatchContractAction } = useContractContext();
    const projectId = currentContext?.externalId;
    const contractId = contract?.id;

    const approveRequestsAsync = React.useCallback(
        async (projectId: string, contractId: string, requests: PersonnelRequest[]) => {
            setApprovedError(null);
            setIsApproving(true);
            try {
                const responses = requests.map(
                    async request =>
                        await apiClient.approveRequestAsync(projectId, contractId, request.id)
                );
                const approvedRequests = await Promise.all(responses);
                
                if (approvedRequests.length > 0) {
                    dispatchContractAction({
                        verb: 'merge',
                        collection: 'activeRequests',
                        payload: approvedRequests,
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

    const approve = React.useCallback(async () => {
        if (!canEdit || !projectId || !contractId || requests.length <= 0) {
            return;
        }

        const userApproval = await sendNotification({
            level: 'high',
            title: `Are you sure you want to approve ${requests.length} requests?`,
        });
        if (userApproval.confirmed) {
            await approveRequestsAsync(projectId, contractId, requests);
        }
    }, [projectId, contractId, canEdit, requests]);

    return { approve, canApprove: canEdit, isApproving, approvedError };
};
