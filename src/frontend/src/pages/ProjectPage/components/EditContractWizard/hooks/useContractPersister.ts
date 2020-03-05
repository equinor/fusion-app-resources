import Contract from '../../../../../models/contract';
import { useAppContext } from '../../../../../appContext';
import { useCurrentContext } from '@equinor/fusion';
import { useCallback, useState } from 'react';

const useContractPersister = (formState: Contract) => {
    const { apiClient } = useAppContext();
    const project = useCurrentContext() as any;
    const [isSaving, setIsSaving] = useState(false);

    const saveAsync = useCallback(async () => {
        setIsSaving(true);

        try {
            if (formState.id) {
                const updatedContract = await apiClient.updateContractAsync(
                    project.externalId,
                    formState.id,
                    formState
                );

                return updatedContract;
            }

            return await apiClient.createContractAsync(project.externalId, formState);
        } catch (e) {
            throw e;
        } finally {
            setIsSaving(false);
        }
    }, [formState]);

    return { saveAsync, isSaving };
};

export default useContractPersister;
