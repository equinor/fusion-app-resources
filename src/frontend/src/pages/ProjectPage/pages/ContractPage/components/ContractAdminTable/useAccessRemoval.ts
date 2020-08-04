import * as React from 'react';
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
    const [isRemoving, setIsRemoving] = React.useState<boolean>(false);
    const [removalError, setRemovalError] = React.useState<Error | null>(null);

    const removeAccessAsync = React.useCallback(
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
            } finally {
                setIsRemoving(false);
            }
        },
        [admins, dispatchContractAction, apiClient]
    );

    const sendNotification = useNotificationCenter();
    const removeAccess = React.useCallback(async () => {
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
