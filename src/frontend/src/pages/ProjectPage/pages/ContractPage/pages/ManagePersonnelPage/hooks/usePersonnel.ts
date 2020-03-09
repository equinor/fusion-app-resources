import * as React from 'react';
import Personnel from '../../../../../../../models/Personnel';
import { useAppContext } from '../../../../../../../appContext';

const usePersonnel = (contractId?: string, projectId?: string) => {
    const [personnel, setPersonnel] = React.useState<Personnel[]>([]);
    const [isFetchingPersonnel, setIsFetchingPersonnel] = React.useState(false);
    const [personnelError, setPersonnelError] = React.useState<Error | null>(null);
    const { apiClient } = useAppContext();

    const fetchPersonnel = async (contract: string, project: string) => {
        setIsFetchingPersonnel(true);
        setPersonnelError(null);
        try {
            const response = await apiClient.getPersonnelAsync(project, contract)
            setPersonnel(response.data.value);
        } catch (e) {
            setPersonnelError(e);
        }

        setIsFetchingPersonnel(false);
    };

    React.useEffect(() => {
        if (!contractId || !projectId) {
            setPersonnel([]);
            return;
        }
        fetchPersonnel(contractId, projectId);
    }, [contractId, projectId]);

    return {
        personnel,
        isFetchingPersonnel,
        personnelError,
    };
};

export default usePersonnel;




