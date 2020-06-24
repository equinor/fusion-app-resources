import * as React from 'react';
import { useAppContext } from '../../../../../appContext';
import { useApiClients } from '@equinor/fusion';
import useReducerCollection from '../../../../../hooks/useReducerCollection';

const useAllPositions = (projectId?: string) => {
    const { appState, dispatchAppAction } = useAppContext();
    const apiClients = useApiClients();

    const fetchPositions = React.useCallback(async () => {
        if (!projectId) {
            return [];
        }
        const result = await apiClients.org.getPositionsAsync(projectId);

        return result.data;
    }, [projectId]);

    const { data, isFetching, error } = useReducerCollection(
        appState,
        dispatchAppAction,
        'positions',
        fetchPositions
    );

    return {
        personnel: data,
        isFetchingPersonnel: isFetching,
        personnelError: error,
    };
};

export default useAllPositions;
