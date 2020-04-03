import * as React from 'react';
import { useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import { useAppContext } from '../../../../../appContext';
import { useContractContext } from '../../../../../contractContex';
import PersonnelRequest from '../../../../../models/PersonnelRequest';
import useEditableRequests from './useEditableRequests';

export default (requests: PersonnelRequest[], onReject?: () => void) => {
    const [isRejecting, setIsRejecting] = React.useState<boolean>(false);
    const [rejectedError, setRejectedError] = React.useState<Error | null>(null);
    const { apiClient } = useAppContext();
    const sendNotification = useNotificationCenter();
    const { canEdit, checkForEditAccessAsync } = useEditableRequests(requests, 'reject');
    const currentContext = useCurrentContext();
    const { contract, dispatchContractAction } = useContractContext();
    const projectId = currentContext?.externalId;
    const contractId = contract?.id;

    const rejectRequestsAsync = React.useCallback(
        async (
            projectId: string,
            contractId: string,
            requests: PersonnelRequest[],
            reason: string
        ) => {
            setRejectedError(null);
            setIsRejecting(true);
            try {
                const responses = requests.map(
                    async request =>
                        await apiClient.rejectRequestAsync(
                            projectId,
                            contractId,
                            request.id,
                            reason
                        )
                );
                const rejectedRequests = await Promise.all(responses);

                dispatchContractAction({
                    verb: 'delete',
                    collection: 'activeRequests',
                    payload: rejectedRequests,
                });

                sendNotification({
                    level: 'low',
                    title: `Request for ${requests
                        .map(r => r.position?.basePosition?.name || '')
                        .join(', ')} has been rejected`,
                });
                await checkForEditAccessAsync(projectId, contractId, requests);
            } catch (e) {
                setRejectedError(e);
                sendNotification({
                    level: 'high',
                    title: 'Failed to reject request(s)',
                });
            }
            setIsRejecting(false);
        },
        [apiClient]
    );

    const reject = React.useCallback(
        async (reason: string) => {
            if (!canEdit || !projectId || !contractId || requests.length <= 0) {
                return;
            }

            await rejectRequestsAsync(projectId, contractId, requests, reason);
            onReject && onReject()
        },
        [projectId, contractId, canEdit, requests, onReject]
    );

    return { reject, checkForEditAccessAsync, canReject: canEdit, isRejecting, rejectedError };
};
