import * as React from 'react';
import { useAppContext } from '../../../../../../../appContext';
import { useContractContext } from '../../../../../../../contractContex';
import useReducerCollection from '../../../../../../../hooks/useReducerCollection';

const usePersonnel = (contractId?: string, projectId?: string) => {
    const { apiClient } = useAppContext();
    const { contractState, dispatchContractAction } = useContractContext();

    const getPersonnelWithPositionsAsync = async () => {
        if (!contractId || !projectId) {
            return;
        }

        const result = await apiClient.getPersonnelWithPositionsAsync(projectId, contractId);
        dispatchContractAction({
            verb: "merge",
            collection: "personnel",
            payload: result,
        });
    };

    const fetchPersonnel = React.useCallback(async () => {
        if (!projectId || !contractId) {
            return [];
        }

        const result = apiClient.getPersonnelAsync(projectId, contractId);
        
        getPersonnelWithPositionsAsync();

        return result;
    }, [projectId, contractId]);

    const { data, isFetching, error } = useReducerCollection(
        contractState,
        dispatchContractAction,
        'personnel',
        fetchPersonnel
    );

    return {
        personnel: data,
        isFetchingPersonnel: isFetching,
        personnelError: error,
    };
};

export default usePersonnel;
