import { useAppContext } from '../../../../../appContext';
import { useNotificationCenter, useCurrentContext, useTelemetryLogger, Position } from '@equinor/fusion';
import { useCallback, useState } from 'react';
import CreatePositionRequest from '../../../../../models/createPositionRequest';
import Contract from '../../../../../models/contract';

const usePositionPersister = (
    formState: CreatePositionRequest,
    contract: Contract,
    repType: string,
    onComplete: (position: Position) => void,
    onClose: () => void
) => {
    const { apiClient } = useAppContext();
    const sendNotification = useNotificationCenter();
    const currentContext = useCurrentContext();
    const [isSaving, setIsSaving] = useState(false);
    const telemetryLogger = useTelemetryLogger();

    const saveAsync = useCallback(async () => {
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

        setIsSaving(true);

        try {
            if (repType === 'company-rep') {
                const position = await apiClient.createExternalCompanyRepresentativeAsync(
                    currentContext.id,
                    contract.id,
                    request
                );

                onComplete(position);
            } else if (repType === 'contract-responsible') {
                const position = await apiClient.createExternalContractResponsibleAsync(
                    currentContext.id,
                    contract.id,
                    request
                );

                onComplete(position);
            }

            onClose();
        } catch (e) {
            telemetryLogger.trackException(e);
            sendNotification({
                level: 'medium',
                title: 'Unable to save position. Please try again',
            });
        }

        setIsSaving(false);
    }, [repType, formState, currentContext, sendNotification, apiClient]);

    return { saveAsync, isSaving };
};

export default usePositionPersister;
