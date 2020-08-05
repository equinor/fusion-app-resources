import { PersonDetails, useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import * as React from 'react';
import {
    PersonDelegationClassification,
    PersonDelegationRequest,
} from '../../../../../../models/PersonDelegation';
import { useAppContext } from '../../../../../../appContext';
import { useContractContext } from '../../../../../../contractContex';

export default (
    toDate: Date | null,
    persons: PersonDetails[],
    accountType: PersonDelegationClassification,
    onSuccessfullyDelegated?: () => void
) => {
    const { apiClient } = useAppContext();
    const { contract, dispatchContractAction } = useContractContext();
    const currentContext = useCurrentContext();

    const [isDelegatingAccess, setIsDelegatingAccess] = React.useState<boolean>(false);
    const [delegateError, setDelegateError] = React.useState<Error | null>(null);
    const sendNotification = useNotificationCenter();

    const delegateAccessAsync = React.useCallback(
        async (projectId: string, contractId: string, validTo: Date) => {
            setIsDelegatingAccess(true);
            setDelegateError(null);
            try {
                const response = persons.map((person) => {
                    const payload: PersonDelegationRequest = {
                        classification: accountType,
                        person: {
                            azureUniquePersonId: person.azureUniqueId,
                        },
                        type: 'CR',
                        validTo,
                    };
                    return apiClient.createPersonRoleDelegationAsync(
                        projectId,
                        contractId,
                        payload
                    );
                });
                const delegatedPersons = await Promise.all(response);
                dispatchContractAction({
                    verb: 'merge',
                    collection: 'administrators',
                    payload: delegatedPersons,
                });
                sendNotification({
                    level: 'low',
                    title: 'Delegation was successful and the person(s) has been notified',
                });
                onSuccessfullyDelegated && onSuccessfullyDelegated();
            } catch (e) {
                setDelegateError(e);
                sendNotification({
                    level: 'high',
                    title: 'Unable to delegate new person(s)',
                    body: e?.response?.error?.message || '',
                });
            } finally {
                setIsDelegatingAccess(false);
            }
        },
        [persons, accountType, apiClient, sendNotification, onSuccessfullyDelegated]
    );

    const delegateAccess = React.useCallback(async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;

        if (contractId && projectId && toDate) {
            await delegateAccessAsync(projectId, contractId, toDate);
        }
    }, [contract, currentContext, toDate, delegateAccessAsync]);

    return { isDelegatingAccess, delegateError, delegateAccess };
};
