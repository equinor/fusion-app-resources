import Contract from '../../../../../models/contract';
import { useAppContext } from '../../../../../appContext';
import { useCurrentContext } from '@equinor/fusion';
import { useCallback, useState } from 'react';

const useContractPersister = (formState: Contract, onSave: (contract: Contract) => void) => {
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
            onSave(updatedContract);
        } else {
            const createdContract = await apiClient.createContractAsync(
                project.externalId,
                formState
            );
            onSave(createdContract);
        }
        setIsSaving(false);
    }, [formState]);

    return { saveAsync, isSaving };
};

export default useContractPersister;
