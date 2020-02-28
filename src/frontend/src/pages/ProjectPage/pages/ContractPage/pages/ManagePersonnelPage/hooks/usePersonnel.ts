import * as React from 'react';
import {
    useFusionContext,
} from '@equinor/fusion';
import ApiClient from '../../../../../../../api/ApiClient';
import Personnel from '../../../../../../../models/Personnel';

const usePersonnel = (contractId?: string,projectId?:string) => {
    const fusionContext = useFusionContext();
    const api = new ApiClient(fusionContext.http.client,"https://resources-api.ci.fusion-dev.net")
    

    const [personnel, setPersonnel] = React.useState<Personnel[]>([]);
    const [isFetchingPersonnel, setIsFetchingPersonnel] = React.useState(false);
    const [personnelError, setPersonnelError] = React.useState<Error | null>(null);

    
    
    React.useEffect(() => {
        fusionContext.auth.container.registerAppAsync('5a842df8-3238-415d-b168-9f16a6a6031b', [
            'https://resources-api.ci.fusion-dev.net/',
        ]);
    }, []);
    
    const fetchPersonnel = async (contract: string,project:string) => {
        setIsFetchingPersonnel(true);
        try {
            console.log("fetch personnel")
            const response = await api.personnel(project,contract)
            console.log("response:", response)
            setPersonnel(response.data);
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
        fetchPersonnel(contractId,projectId);
    }, [contractId,projectId]);

    return {
        personnel,
        isFetchingPersonnel,
        personnelError,
    };
};

export default usePersonnel;




