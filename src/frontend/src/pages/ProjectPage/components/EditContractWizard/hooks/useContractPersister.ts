import Contract from '../../../../../models/contract';
import { useAppContext } from '../../../../../appContext';
import { useCurrentContext } from '@equinor/fusion';
import { useCallback, useState } from 'react';

const useContractPersister = (formState: Contract) => {
    const { apiClient, dispatchAppAction } = useAppContext();
    const project = useCurrentContext() as any;
    const [isSaving, setIsSaving] = useState(false);

    const saveAsync = useCallback(async () => {
        setIsSaving(true);

        try {
            const contract = formState.id
                ? await apiClient.updateContractAsync(project.externalId, formState.id, formState)
                : await apiClient.createContractAsync(project.externalId, formState);

            dispatchAppAction({
                verb: 'merge',
                collection: 'contracts',
                payload: [contract],
            });

            return contract;
        } catch (e) {
            throw e;
        } finally {
            setIsSaving(false);
        }
    }, [formState]);

    return { saveAsync, isSaving };
};

export default useContractPersister;
