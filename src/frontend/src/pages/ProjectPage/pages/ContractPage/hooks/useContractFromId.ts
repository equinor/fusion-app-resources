import { useState, useEffect, useCallback } from 'react';
import Contract from '../../../../../models/contract';
import { useAppContext } from '../../../../../appContext';
import { useCurrentContext, useDebouncedAbortable } from '@equinor/fusion';
import useReducerCollection from '../../../../../hooks/useReducerCollection';
import ResourceError from '../../../../../reducers/ResourceError';

const useContractFromId = (id: string) => {
    const { apiClient, appState, dispatchAppAction } = useAppContext();
    const { data } = useReducerCollection(appState, dispatchAppAction, 'contracts');

    const [contract, setContract] = useState<Contract | null>(
        data.find((c) => c.id === id) || null
    );
    const [isFetchingContract, setIsFetchingContract] = useState(false);
    const [contractError, setContractError] = useState<ResourceError | null>(null);

    const currentContext = useCurrentContext();
    const fetchContract = useCallback(async () => {
        if (!id || !currentContext) {
            return;
        }

        try {
            setIsFetchingContract(true);
            setContractError(null);
            const contractResponse = await apiClient.getContractAsync(currentContext.id, id);
            setContract(contractResponse);
            dispatchAppAction({
                verb: 'merge',
                collection: 'contracts',
                payload: [contractResponse],
            });
        } catch (e) {
            setContractError(e);
        }

        setIsFetchingContract(false);
    }, [id, currentContext, apiClient]);

    useDebouncedAbortable(fetchContract, void 0, 0);

    return { contract, isFetchingContract, contractError };
};

export default useContractFromId;
