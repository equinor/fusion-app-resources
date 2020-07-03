import { PersonDetails, useCurrentContext } from '@equinor/fusion';
import * as React from 'react';
import { PersonDelegationClassification } from '../../../../../../models/PersonDelegation';
import { useAppContext } from '../../../../../../appContext';
import { useContractContext } from '../../../../../../contractContex';

export default (
    toDate: Date,
    toPersons: PersonDetails,
    accountType: PersonDelegationClassification
) => {
    const { apiClient } = useAppContext();
    const { contract } = useContractContext();
    const currentContext = useCurrentContext();

    const [isDelegatingAccess, setIsDelegatingAccess] = React.useState<boolean>(false);
    const [delegateError, setDelegateError] = React.useState<Error | null>(null);

    const delegateAccessAsync = React.useCallback((projectId: string, contractId: string) => {
        setIsDelegatingAccess(true);
        setDelegateError(null);
        try {
            const response = await apiClient.
        }
    }, []);

    React.useEffect(() => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (contractId && projectId) {
            delegateAccessAsync(contractId, projectId);
        }
    }, [contract, currentContext]);
};
