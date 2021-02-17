import { useCallback } from 'react';
import { useAppContext } from '../../../../../appContext';
import useReducerCollection from '../../../../../hooks/useReducerCollection';

const useContracts = (projectId?: string) => {
    const { apiClient, appState, dispatchAppAction } = useAppContext();

    const fetchContracts = useCallback(async () => {
        if (!projectId) {
            return [];
        }

        return apiClient.getContractsAsync(projectId);
    }, [projectId]);

    const contracts = useReducerCollection(appState, dispatchAppAction, 'contracts', fetchContracts, 'set');

    return {
        contracts: contracts.data,
        isFetchingContracts: contracts.isFetching,
        contractsError: contracts.error,
    };
};

export default useContracts;
