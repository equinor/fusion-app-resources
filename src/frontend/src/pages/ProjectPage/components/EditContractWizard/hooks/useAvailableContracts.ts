import { useState, useEffect } from 'react';
import AvailableContract from '../../../../../models/availableContract';
import { useCurrentContext } from '@equinor/fusion';
import { useAppContext } from '../../../../../appContext';

const useAvailableContracts = () => {
    const [availableContracts, setAvailableContracts] = useState<AvailableContract[]>([]);
    const [isFetchingAvailableContracts, setIsFetchingAvailableContracts] = useState(false);

    const currentContext = useCurrentContext();
    const { apiClient } = useAppContext();
    const fetchAvailableContracts = async () => {
        if (!currentContext) {
            return;
        }

        setIsFetchingAvailableContracts(true);

        try {
            const availableContract = await apiClient.getAvailableContractsAsync(currentContext.id);
            setAvailableContracts(availableContract);
        } catch (e) {
            console.error(e);
        }

        setIsFetchingAvailableContracts(false);
    };

    useEffect(() => {
        fetchAvailableContracts();
    }, [currentContext]);

    return {
        availableContracts,
        isFetchingAvailableContracts,
    };
};

export default useAvailableContracts;
