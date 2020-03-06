import * as React from 'react';
import { useAppContext } from '../../../../../appContext';
import { useReducerCollection } from '../../../../../appReducer';

const useContracts = (projectId?: string) => {
    const { apiClient } = useAppContext();

    const fetchContracts = React.useCallback(async () => {
        if (!projectId) {
            return [];
        }

        return apiClient.getContractsAsync(projectId);
    }, [projectId]);

    const contracts = useReducerCollection('contracts', fetchContracts);

    return {
        contracts: contracts.data,
        isFetchingContracts: contracts.isFetching,
        contractsError: contracts.error,
    };
};

export default useContracts;
