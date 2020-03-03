import { useState, useEffect } from 'react';
import Company from '../../../../../models/company';
import { useApiClients } from '@equinor/fusion';

const useCompanies = () => {
    const [companies, setCompanies] = useState<Company[]>([]);
    const [isFetchingCompanies, setIsFetchingCompanies] = useState(false);
    const [companiesError, setCompaniesError] = useState<Error | null>(null);

    const apiClients = useApiClients();
    const fetchAvailableContracts = async () => {
        setIsFetchingCompanies(true);
        setCompaniesError(null);

        try {
            const response = await apiClients.people.getAsync<Company[]>('companies');
            setCompanies(response.data.map(c => ({ ...c, identifier: c.id })));
        } catch (e) {
            setCompaniesError(e);
        }

        setIsFetchingCompanies(false);
    };

    useEffect(() => {
        fetchAvailableContracts();
    }, []);

    return {
        companies,
        isFetchingCompanies,
        companiesError,
    };
};

export default useCompanies;
