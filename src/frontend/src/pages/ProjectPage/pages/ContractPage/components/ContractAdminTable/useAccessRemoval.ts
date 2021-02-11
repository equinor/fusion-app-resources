import { useState, useCallback } from 'react';
import { useNotificationCenter, useCurrentContext } from '@equinor/fusion';
import PersonDelegation, {
    PersonDelegationClassification,
} from '../../../../../../models/PersonDelegation';
import { useAppContext } from '../../../../../../appContext';
import { useContractContext } from '../../../../../../contractContex';

export default (accountType: PersonDelegationClassification, admins: PersonDelegation[]) => {
    const { apiClient } = useAppContext();
    const { dispatchContractAction, contract } = useContractContext();
    const currentContext = useCurrentContext();
    const [isRemoving, setIsRemoving] = useState<boolean>(false);
    const [removalError, setRemovalError] = useState<Error | null>(null);

    const removeAccessAsync = useCallback(
        async (projectId: string, contractId: string) => {
            setIsRemoving(true);
            setRemovalError(null);
            try {
                const requests = admins.map((a) =>
                    apiClient.deletePersonRoleDelegationAsync(projectId, contractId, a.id)
                );
                await Promise.all(requests);
                dispatchContractAction({
                    collection: 'administrators',
                    verb: 'delete',
                    payload: admins,
                });
            } catch (e) {
                setRemovalError(e);
                sendNotification({
                    level: 'high',
                    title: 'Unable to remove new person(s)',
                    body: e?.response?.error?.message || ""
                });
            } finally {
                setIsRemoving(false);
            }
        },
        [admins, dispatchContractAction, apiClient]
    );

    const sendNotification = useNotificationCenter();
    const removeAccess = useCallback(async () => {
        const response = await sendNotification({
            level: 'high',
            title: 'Remove delegated access',
            confirmLabel: "Yes, I'm sure",
            cancelLabel: 'Cancel',
            body: `Are you sure you want to delegated role of ${
                accountType === 'Internal' ? 'Equinor' : accountType
            } admin for ${admins.map((a) => a.person.name).join(', ')}?`,
        });
        const contractId = contract?.id;
        const projectId = currentContext?.id;

        if (response.confirmed && contractId && projectId) {
            await removeAccessAsync(projectId, contractId);
        }
    }, [accountType, admins, sendNotification, contract, currentContext]);

    return { removeAccess, isRemoving, removalError };
};
