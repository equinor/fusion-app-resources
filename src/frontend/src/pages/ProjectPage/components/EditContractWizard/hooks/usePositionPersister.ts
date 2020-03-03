import { useAppContext } from '../../../../../appContext';
import { useNotificationCenter, useCurrentContext } from '@equinor/fusion';
import { useCallback } from 'react';
import CreatePositionRequest from '../../../../../models/createPositionRequest';
import Contract from '../../../../../models/contract';

const usePositionPersister = (
    formState: CreatePositionRequest,
    contract: Contract,
    repType: string,
    onComplete: (positionId: string) => void,
    onClose: () => void
) => {
    const { apiClient } = useAppContext();
    const sendNotification = useNotificationCenter();
    const currentContext = useCurrentContext();

    const onSave = useCallback(async () => {
        const request = { ...formState };

        if (!currentContext?.id) {
            sendNotification({
                level: 'medium',
                title: 'No context selected',
            });
            return;
        } else if (!contract.id) {
            sendNotification({
                level: 'medium',
                title: "Can't create position on non existing contract",
            });
            return;
        }

        if (repType === 'company-rep') {
            const position = await apiClient.createExternalCompanyReprasentativeAsync(
                currentContext.id,
                contract.id,
                request
            );

            onComplete(position.id);
        } else if (repType === 'contract-responsible') {
            const position = await apiClient.createExternalContractResponsibleAsync(
                currentContext.id,
                contract.id,
                request
            );

            onComplete(position.id);
        }

        onClose();
    }, [repType, formState, currentContext, sendNotification, apiClient]);

    return onSave;
};

export default usePositionPersister;
