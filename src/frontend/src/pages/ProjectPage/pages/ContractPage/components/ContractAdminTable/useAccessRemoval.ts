import * as React from 'react';
import { useNotificationCenter } from '@equinor/fusion';
import { PersonDelegationClassification } from '../../../../../../models/PersonDelegation';

export default (accountType: PersonDelegationClassification, persons: string[]) => {
    const removeAccessAsync = React.useCallback(async () => {}, [persons]);

    const sendNotification = useNotificationCenter();
    const removeAccess = React.useCallback(async () => {
        const response = await sendNotification({
            level: 'high',
            title: 'Remove delegated access',
            confirmLabel: "Yes, I'm sure",
            cancelLabel: 'Cancel',
            body: `Are you sure you want to delegated role of ${
                accountType === 'Internal' ? 'Equinor' : accountType
            } admin for ${persons.join(', ')}?`,
        });
        if (response.confirmed) {
            await removeAccessAsync();
        }
    }, [accountType, persons, sendNotification]);

    return { removeAccess };
};
