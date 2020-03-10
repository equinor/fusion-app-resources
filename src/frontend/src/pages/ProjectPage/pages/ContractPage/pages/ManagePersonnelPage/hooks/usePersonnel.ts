import * as React from 'react';
import { useAppContext } from '../../../../../../../appContext';
import { useContractContext } from '../../../../../../../contractContex';
import useReducerCollection from '../../../../../../../hooks/useReducerCollection';

const usePersonnel = (contractId?: string, projectId?: string) => {
    const { apiClient } = useAppContext();
    const { contractState, dispatchContractAction } = useContractContext();

    const fetchPersonnel = React.useCallback(async () => {
        if (!projectId || !contractId) {
            return [];
        }

        return apiClient.getPersonnelAsync(projectId, contractId);
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
