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

        if (formState.id) {
            const updatedContract = await apiClient.updateContractAsync(
                project.externalId,
                formState.id,
                formState
            );

            setIsSaving(false);

            return updatedContract;
        }

        const createdContract = await apiClient.createContractAsync(project.externalId, formState);

        setIsSaving(false);

        return createdContract;
    }, [formState]);

    return { saveAsync, isSaving };
};

export default useContractPersister;
