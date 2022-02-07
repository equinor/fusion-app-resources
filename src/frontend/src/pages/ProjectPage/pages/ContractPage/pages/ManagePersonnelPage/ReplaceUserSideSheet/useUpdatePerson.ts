import { PersonDetails, useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import { useCallback, useState } from 'react';
import { useAppContext } from '../../../../../../../appContext';
import { useContractContext } from '../../../../../../../contractContex';
import Personnel from '../../../../../../../models/Personnel';
import { ReplacePersonRequest } from '../../../../../../../models/ReplacePersonRequest';

const useUpdatePerson = (
    updateToPerson: PersonDetails | null,
    currentPersonnel: Personnel,
    onReplacementSuccess: () => void
) => {
    const [isUpdatingPerson, setIsUpdatingPerson] = useState<boolean>(false);

    const { apiClient } = useAppContext();
    const currentContext = useCurrentContext();
    const { contract, dispatchContractAction } = useContractContext();
    const sendNotification = useNotificationCenter();

    const updatePersonAsync = useCallback(
        async (
            projectId: string,
            contractId: string,
            currentPersonId: string,
            currentPersonUpn: string | null,
            payload: ReplacePersonRequest
        ) => {
            setIsUpdatingPerson(true);
            //Need the add force flag if a person upn is replaced
            const tryForce = currentPersonUpn !== payload.upn;
            try {
                const response = await apiClient.replacePersonAsync(
                    projectId,
                    contractId,
                    currentPersonId,
                    payload,
                    tryForce
                );
                sendNotification({
                    level: 'low',
                    title: 'Person instance successfully updated!',
                    cancelLabel: 'dismiss',
                });

                dispatchContractAction({
                    verb: 'delete',
                    collection: 'personnel',
                    payload: [currentPersonnel],
                });

                dispatchContractAction({
                    verb: 'merge',
                    collection: 'personnel',
                    payload: [response],
                });
                onReplacementSuccess();
            } catch (e: any) {
                const errorCode = e?.statusCode;
                const errorMessage = e?.response?.error?.message;

                sendNotification({
                    level: 'high',
                    title: `[${errorCode}] - An error occurred`,
                    body: errorMessage,
                });
            } finally {
                setIsUpdatingPerson(false);
            }
        },
        [sendNotification, dispatchContractAction, currentPersonnel, onReplacementSuccess]
    );

    const updatePerson = useCallback(() => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        const azureUniquePersonId = updateToPerson?.azureUniqueId;
        const upn = updateToPerson?.upn;
        const currentPersonId = currentPersonnel.azureUniquePersonId;
        const currentPersonUpn = currentPersonnel.upn || null;

        if (contractId && projectId && azureUniquePersonId && upn && currentPersonId) {
            const payload: ReplacePersonRequest = { azureUniquePersonId, upn };

            updatePersonAsync(projectId, contractId, currentPersonId, currentPersonUpn, payload);
        }
    }, [contract, currentContext, updateToPerson, currentPersonnel, updatePersonAsync]);

    return {
        updatePerson,
        isUpdatingPerson,
    };
};
export default useUpdatePerson;
