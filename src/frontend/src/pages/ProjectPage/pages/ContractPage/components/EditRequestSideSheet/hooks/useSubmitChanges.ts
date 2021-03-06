import { useState, useCallback } from 'react';
import {
    useNotificationCenter,
    useCurrentContext,
    HttpClientRequestFailedError,
    FusionApiHttpErrorResponse,
} from '@equinor/fusion';
import { transformToCreatePersonnelRequest } from '../utils';
import PersonnelRequest from '../../../../../../../models/PersonnelRequest';
import { useAppContext } from '../../../../../../../appContext';
import { EditRequest } from '..';
import { useContractContext } from '../../../../../../../contractContex';
import {
    FailedRequest,
    SuccessfulRequest,
} from '../../../../../../../components/RequestProgressSidesheet';
import RequestValidationError from '../../../../../../../models/RequestValidationError';

export default (formState: EditRequest[]) => {
    const { contract, dispatchContractAction } = useContractContext();
    const currentContext = useCurrentContext();
    const sendNotification = useNotificationCenter();
    const { apiClient } = useAppContext();

    const [pendingRequests, setPendingRequests] = useState<EditRequest[]>([]);
    const [failedRequests, setFailedRequests] = useState<FailedRequest<EditRequest>[]>([]);
    const [successfulRequests, setSuccessfulRequests] = useState<
        SuccessfulRequest<EditRequest, PersonnelRequest>[]
    >([]);

    const createRequest = useCallback(
        async (projectId: string, contractId: string, request: EditRequest) => {
            const transformedRequest = transformToCreatePersonnelRequest(request);

            try {
                setPendingRequests(r => [...r, request]);
                if (transformedRequest.id) {
                    const updateResponse = await apiClient.updatePersonnelRequestAsync(
                        projectId,
                        contractId,
                        transformedRequest.id,
                        transformedRequest
                    );

                    setSuccessfulRequests(s => [
                        ...s,
                        {
                            item: request,
                            response: updateResponse,
                        },
                    ]);
                    dispatchContractAction({
                        verb: 'merge',
                        collection: 'activeRequests',
                        payload: [updateResponse],
                    });
                } else {
                    const createResponse = await apiClient.createPersonnelRequestAsync(
                        projectId,
                        contractId,
                        transformedRequest
                    );

                    setSuccessfulRequests(s => [
                        ...s,
                        {
                            item: request,
                            response: createResponse,
                        },
                    ]);
                    dispatchContractAction({
                        verb: 'merge',
                        collection: 'activeRequests',
                        payload: [createResponse],
                    });
                }
            } catch (error) {
                if (error instanceof HttpClientRequestFailedError) {
                    const requestError = error as HttpClientRequestFailedError<
                        RequestValidationError
                    >;
                    setFailedRequests(f => [
                        ...f,
                        {
                            error: requestError.response,
                            item: request,
                            isEditable:
                                requestError.statusCode <= 500 &&
                                requestError.statusCode !== 424 &&
                                requestError.statusCode !== 408,
                        },
                    ]);
                } else {
                    setFailedRequests(f => [
                        ...f,
                        {
                            error,
                            item: request,
                            isEditable: false,
                        },
                    ]);
                }
            }

            setPendingRequests(r => r.filter(x => x !== request));
        },
        [apiClient]
    );

    const reset = useCallback(() => {
        setPendingRequests([]);
        setFailedRequests([]);
        setSuccessfulRequests([]);
    }, []);

    const submit = useCallback(() => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        reset();

        if (contractId && projectId) {
            formState.map(request => createRequest(projectId, contractId, request));
        } else {
            sendNotification({
                level: 'medium',
                title: 'Cannot find contract or project',
                priority: 'high',
            });
        }
    }, [contract, currentContext, createRequest, formState, reset]);

    const removeFailedRequest = useCallback((request: FailedRequest<EditRequest>) => {
        setFailedRequests(fr => fr.filter(r => r !== request));
    }, []);

    return {
        submit,
        reset,
        pendingRequests,
        failedRequests,
        successfulRequests,
        removeFailedRequest,
    };
};
