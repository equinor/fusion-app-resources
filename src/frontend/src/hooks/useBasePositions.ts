import { useCallback } from 'react';
import { useApiClients, BasePosition, combineUrls } from '@equinor/fusion';
import useReducerCollection from './useReducerCollection';
import { useContractContext } from '../contractContex';

export type useBasePositionsContext = {
    basePositions: BasePosition[];
    isFetchingBasePositions: Boolean;
    basePositionsError: Error | null;
};

const useBasePositions = (): useBasePositionsContext => {
    const { contract, contractState, dispatchContractAction } = useContractContext();
    const apiClients = useApiClients();
    const fetchBasePositionsAsync = useCallback(async () => {
        if (!contract?.id) return []

        const positions = await apiClients.org.getAsync<BasePosition[]>(
            combineUrls('positions', "basepositions?$filter=projectType eq 'PRD-Contracts'"))

        return positions.data
    }, [contract]);

    const { data: basePositions, isFetching: isFetchingBasePositions, error: basePositionsError } = useReducerCollection(
        contractState,
        dispatchContractAction,
        'basePositions',
        fetchBasePositionsAsync
    );

    return {
        basePositions,
        isFetchingBasePositions,
        basePositionsError,
    };
};

export default useBasePositions;
