import * as React from 'react';
import { AccountType } from '.';
import { useNotificationCenter } from '@equinor/fusion';

export default (accountType: AccountType, persons: string[]) => {
    const removeAccessAsync = React.useCallback(async () => {}, [persons]);

    const sendNotification = useNotificationCenter();
    const removeAccess = React.useCallback(async () => {
        const response = await sendNotification({
            level: 'high',
            title: 'Remove delegated access',
            confirmLabel: "Yes, I'm sure",
            cancelLabel: 'Cancel',
            body: `Are you sure you want to delegated role of ${
                accountType === 'external' ? 'External' : 'Equinor'
            } admin for ${persons.join(', ')}?`,
        });
        if (response.confirmed) {
            await removeAccessAsync();
        }
    }, [accountType, persons, sendNotification]);

    return { removeAccess };
};
