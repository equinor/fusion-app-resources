import * as React from 'react';
import {
    useFusionContext,
    combineUrls,
    FusionApiErrorMessage,
} from '@equinor/fusion';
import Contract from '../../../../../models/contract';

type ContractResponse = {
    value: Contract[];
};

const useContracts = (projectId?: string) => {
    const [contracts, setContracts] = React.useState<Contract[]>([]);
    const [isFetchingContracts, setIsFetchingContracts] = React.useState(false);
    const [contractsError, setContractsError] = React.useState<Error | null>(null);

    const fusionContext = useFusionContext();
    React.useEffect(() => {
        fusionContext.auth.container.registerAppAsync('5a842df8-3238-415d-b168-9f16a6a6031b', [
            'https://resources-api.ci.fusion-dev.net/',
        ]);
    }, []);

    const fetchContracts = async (id: string) => {
        setIsFetchingContracts(true);
        try {
            // fetch and set contracts
            const response = await fusionContext.http.client.getAsync<
                ContractResponse,
                FusionApiErrorMessage
            >(combineUrls('https://resources-api.ci.fusion-dev.net', 'projects', id, 'contracts'));
            setContracts(response.data.value);
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