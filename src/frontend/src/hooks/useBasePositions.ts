import { useCallback } from 'react';
import { useApiClients, BasePosition, combineUrls } from '@equinor/fusion';
import useReducerCollection from './useReducerCollection';
import { useAppContext } from '../appContext';

export type useBasePositionsContext = {
    basePositions: BasePosition[];
    isFetchingBasePositions: Boolean;
    basePositionsError: Error | null;
};

const useBasePositions = (): useBasePositionsContext => {
    const apiClients = useApiClients();
    const { appState, dispatchAppAction } = useAppContext()
    const fetchBasePositionsAsync = useCallback(async () => {

        const positions = await apiClients.org.getAsync<BasePosition[]>(
            combineUrls('positions', "basepositions?$filter=projectType eq 'PRD-Contracts'"))

        return positions.data
    }, []);

    const { data: basePositions, isFetching: isFetchingBasePositions, error: basePositionsError } = useReducerCollection(
        appState,
        dispatchAppAction,
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
