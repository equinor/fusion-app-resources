import * as React from 'react';
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

export default (formState: EditRequest[]) => {
    const { contract } = useContractContext();
    const currentContext = useCurrentContext();
    const sendNotification = useNotificationCenter();
    const { apiClient } = useAppContext();

    const [pendingRequests, setPendingRequests] = React.useState<EditRequest[]>([]);
    const [failedRequests, setFailedRequests] = React.useState<FailedRequest<EditRequest>[]>([]);
    const [successfulRequests, setSuccessfullRequests] = React.useState<
        SuccessfulRequest<EditRequest, PersonnelRequest>[]
    >([]);

    const createRequest = React.useCallback(
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

                    setSuccessfullRequests(s => [
                        ...s,
                        {
                            item: request,
                            response: updateResponse,
                        },
                    ]);
                } else {
                    const createResponse = await apiClient.createPersonnelRequestAsync(
                        projectId,
                        contractId,
                        transformedRequest
                    );

                    setSuccessfullRequests(s => [
                        ...s,
                        {
                            item: request,
                            response: createResponse,
                        },
                    ]);
                }
            } catch (error) {
                if (error instanceof HttpClientRequestFailedError) {
                    const requestError = error as HttpClientRequestFailedError<
                        FusionApiHttpErrorResponse
                    >;
                    setFailedRequests(f => [
                        ...f,
                        {
                            error: error,
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

    const reset = React.useCallback(() => {
        setPendingRequests([]);
        setFailedRequests([]);
        setSuccessfullRequests([]);
    }, []);

    const submit = React.useCallback(() => {
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

    return { submit, reset, pendingRequests, failedRequests, successfulRequests };
};
