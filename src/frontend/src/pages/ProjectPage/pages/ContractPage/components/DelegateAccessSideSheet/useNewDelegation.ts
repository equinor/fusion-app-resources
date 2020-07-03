import { PersonDetails, useCurrentContext } from '@equinor/fusion';
import * as React from 'react';
import {
    PersonDelegationClassification,
    PersonDelegationRequest,
} from '../../../../../../models/PersonDelegation';
import { useAppContext } from '../../../../../../appContext';
import { useContractContext } from '../../../../../../contractContex';

export default (
    toDate: Date,
    persons: PersonDetails[],
    accountType: PersonDelegationClassification
) => {
    const { apiClient } = useAppContext();
    const { contract } = useContractContext();
    const currentContext = useCurrentContext();

    const [isDelegatingAccess, setIsDelegatingAccess] = React.useState<boolean>(false);
    const [delegateError, setDelegateError] = React.useState<Error | null>(null);

    const delegateAccessAsync = React.useCallback(async (projectId: string, contractId: string) => {
        setIsDelegatingAccess(true);
        setDelegateError(null);
        try {
            const response = persons.map(async (person) => {
                const payload: PersonDelegationRequest = {
                    classification: accountType,
                    person: {
                        azureUniquePersonId: person.azureUniqueId,
                    },
                    type: 'cr',
                    validTo: toDate,
                };
                return apiClient.createPersonRoleDelegationAsync(projectId, contractId, payload);
            });
            const delegatedPersons = await Promise.all(response);
        } catch (e) {
            setDelegateError(e);
        } finally {
            setIsDelegatingAccess(false);
        }
    }, []);

    const delegateAccess = React.useCallback(async() => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (contractId && projectId) {
            await delegateAccessAsync(contractId, projectId);
        }
    }, [contract, currentContext]);

    return { isDelegatingAccess, delegateError, delegateAccess };
};
