import * as React from 'react';
import { useNotificationCenter, useCurrentContext } from '@equinor/fusion';
import CreatePersonnelRequest from '../../../../../../../models/CreatePersonnelRequest';
import { transformToCreatePersonnelRequest } from '../utils';
import PersonnelRequest from '../../../../../../../models/PersonnelRequest';
import { useAppContext } from '../../../../../../../appContext';
import { EditRequest } from '..';
import { useContractContext } from '../../../../../../../contractContex';

type SubmitError = {
    errorMessage: string;
};

export default (
    formState: EditRequest[],
    setEditRequests: React.Dispatch<React.SetStateAction<PersonnelRequest[] | null>>
) => {
    const { contract } = useContractContext();
    const currentContext = useCurrentContext();

    const [isSubmitting, setIsSubmitting] = React.useState<boolean>(false);
    const sendNotification = useNotificationCenter();
    const { apiClient } = useAppContext();

    const submitChangesAsync = React.useCallback(
        async (projectId: string, contractId: string) => {
            setIsSubmitting(true);
            try {
                const failedRequests: CreatePersonnelRequest[] = [];
                const transformedRequests = transformToCreatePersonnelRequest(formState);
                const requestPromises = transformedRequests.reduce(
                    (
                        previousPromise: Promise<PersonnelRequest[]>,
                        request: CreatePersonnelRequest
                    ) => {
                        return previousPromise.then(prevResult => {
                            return (request.id
                                ? apiClient.updatePersonnelRequestAsync(
                                      projectId,
                                      contractId,
                                      request.id,
                                      request
                                  )
                                : apiClient.createPersonnelRequestAsync(
                                      projectId,
                                      contractId,
                                      request
                                  )
                            )
                                .then(newResult => [...prevResult, ...newResult.value])
                                .catch(() => {
                                    failedRequests.push(request);
                                    return prevResult;
                                });
                        });
                    },
                    Promise.resolve([])
                );
                await requestPromises;
                if (failedRequests.length >= 0) {
                    const error: SubmitError = {
                        errorMessage: `Could not submit ${failedRequests
                            .map(request => request.position?.name)
                            .join(', ')}`,
                    };
                    throw error;
                }
                setEditRequests(null);
            } catch (e) {
                const response = await sendNotification({
                    level: 'high',
                    title: 'Unable to submit requests',
                    priority: 'high',
                    body: e && e.errorMessage ? e.errorMessage : '',
                    confirmLabel: 'Try again',
                    cancelLabel: 'Cancel',
                });
                if (response.confirmed) {
                    submitChangesAsync(projectId, contractId);
                }
            } finally {
                setIsSubmitting(false);
            }
        },
        [setIsSubmitting, setEditRequests, sendNotification, formState, apiClient]
    );

    const submit = React.useCallback(() => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (contractId && projectId) {
            submitChangesAsync(projectId, contractId);
        } else {
            sendNotification({
                level: 'medium',
                title: 'Cannot find contract or project',
                priority: 'high',
            });
        }
    }, [contract, currentContext, sendNotification]);

    return { submit, isSubmitting };
};
