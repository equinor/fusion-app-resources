import { useState, useEffect } from 'react';
import Company from '../../../../../models/company';
import { useApiClients } from '@equinor/fusion';

const useCompanies = () => {
    const [companies, setCompanies] = useState<Company[]>([]);
    const [isFetchingCompanies, setIsFetchingCompanies] = useState(false);

    const apiClients = useApiClients();
    const fetchAvailableContracts = async () => {

        setIsFetchingCompanies(true);

        try {
            const response = await apiClients.people.getAsync<Company[]>('companies');
            setCompanies(response.data.map(c => ({ ...c, identifier: c.id })));
        } catch (e) {
            console.error(e);
        }

        setIsFetchingCompanies(false);
    };

    useEffect(() => {
        fetchAvailableContracts();
    }, []);

    return {
        companies,
        isFetchingCompanies,
    };
};

export default useCompanies;
