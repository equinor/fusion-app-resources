import * as React from 'react';
import Contract from '../../../../../models/contract';
import { useAppContext } from '../../../../../appContext';

const useContracts = (projectId?: string) => {
    const [contracts, setContracts] = React.useState<Contract[]>([]);
    const [isFetchingContracts, setIsFetchingContracts] = React.useState(false);
    const [contractsError, setContractsError] = React.useState<Error | null>(null);

    const { apiClient } = useAppContext();
    const fetchContracts = async (id: string) => {
        setIsFetchingContracts(true);
        try {
            // fetch and set contracts
            const contractResult = await apiClient.getContractsAsync(id);
            setContracts(contractResult);
        } catch (e) {
            setContractsError(e);
        }

        setIsFetchingContracts(false);
    };

    React.useEffect(() => {
        if (!projectId) {
            setContracts([]);
            return;
        }

        fetchContracts(projectId);
    }, [projectId]);

    return {
        contracts,
        isFetchingContracts,
        contractsError,
    };
};

export default useContracts;
