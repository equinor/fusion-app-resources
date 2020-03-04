import { useState, useEffect } from 'react';
import Contract from '../../../../../models/contract';
import { useAppContext } from '../../../../../appContext';
import { useCurrentContext } from '@equinor/fusion';

const useContractFromId = (id: string) => {
    const [contract, setContract] = useState<Contract | null>(null);
    const [isFetchingContract, setIsFetchingContract] = useState(false);
    const [contractError, setContractError] = useState<Error | null>(null);

    const { apiClient } = useAppContext();
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
        } catch (e) {
            setContractError(e);
        }

        setIsFetchingContract(false);
    };

    useEffect(() => {
        fetchContract();
    }, [id, currentContext]);

    return { contract, isFetchingContract, contractError };
};

export default useContractFromId;