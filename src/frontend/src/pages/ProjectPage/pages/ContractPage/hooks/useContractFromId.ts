import { useState, useEffect } from 'react';
import Contract from '../../../../../models/contract';
import { useAppContext } from '../../../../../appContext';
import { useCurrentContext, useDebouncedAbortable } from '@equinor/fusion';
import { useReducerCollection } from '../../../../../appReducer';

const useContractFromId = (id: string) => {
    const { data } = useReducerCollection('contracts');

    const [contract, setContract] = useState<Contract | null>(data.find(c => c.id === id) || null);
    const [isFetchingContract, setIsFetchingContract] = useState(false);
    const [contractError, setContractError] = useState<Error | null>(null);

    const { apiClient, dispatchAppAction } = useAppContext();
    const currentContext = useCurrentContext();
    const fetchContract = async () => {
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
    };

    useDebouncedAbortable(fetchContract, void 0, 0);

    return { contract, isFetchingContract, contractError };
};

export default useContractFromId;
