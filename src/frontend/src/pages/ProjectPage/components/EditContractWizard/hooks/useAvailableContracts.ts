import { useState, useEffect } from 'react';
import AvailableContract from '../../../../../models/availableContract';
import { useCurrentContext } from '@equinor/fusion';
import { useAppContext } from '../../../../../appContext';

const useAvailableContracts = () => {
    const [availableContracts, setAvailableContracts] = useState<AvailableContract[]>([]);
    const [isFetchingAvailableContracts, setIsFetchingAvailableContracts] = useState(false);
    const [availableContractsError, setAvailableContractsError] = useState<Error | null>(null);

    const currentContext = useCurrentContext();
    const { apiClient } = useAppContext();
    const fetchAvailableContracts = async () => {
        if (!currentContext) {
            return;
        }

        setAvailableContractsError(null);
        setIsFetchingAvailableContracts(true);

        try {
            const availableContract = await apiClient.getAvailableContractsAsync(currentContext.id);
            setAvailableContracts(availableContract);
        } catch (e) {
            setAvailableContractsError(e);
        }

        setIsFetchingAvailableContracts(false);
    };

    useEffect(() => {
        fetchAvailableContracts();
    }, [currentContext]);

    const mockContract: AvailableContract = {
        contractNumber: new Date().getTime().toString(),
    };

    return {
        availableContracts: [mockContract, ...availableContracts],
        isFetchingAvailableContracts,
        availableContractsError,
    };
};

export default useAvailableContracts;
